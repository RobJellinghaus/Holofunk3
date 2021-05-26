// Copyright (c) 2020 by Rob Jellinghaus.

using LiteNetLib;
using System;
using System.Linq.Expressions;

namespace Distributed.State
{
    /// <summary>
    /// Base class for distributed objects.
    /// </summary>
    /// <remarks>
    /// The DistributedState framework divides the functionality of a distributed object into two parts:
    /// 
    /// 1) A DistributedObject derived class, which handles message routing from/to proxy objects, and which
    ///    also relays messages to:
    /// 2) A LocalObject derived class, which implements the actual local behavior of the distributed object.
    /// 
    /// The LocalObject derived class implements an interface (derived from IDistributedObject) which presents
    /// all the methods that can be invoked on those objects.
    /// 
    /// The DistributedObject derived class also implements that same interface.  If the DistributedObject is
    /// an owner, it will relay calls as commands to its proxies (as well as to its local object); if the
    /// DistributedObject is a proxy, it will relay the message as a command request to the owner.
    /// If the DistributedObject is a proxy and receives a command from the owner, it relays it to the proxy's
    /// local object.
    /// 
    /// The net result is:
    /// 1) All method invocations on the owner are relayed reliably and in sequence to all proxies.
    /// 2) The owner and all proxies update local state in response to that reliable command sequence, keeping
    ///    all proxies synchronized.
    /// 3) Proxies whose methods are invoked do not update any local state, but only relay command requests to
    ///    the owner; the owner is always authoritative about state.
    /// </remarks>
    public abstract class DistributedObject : IDistributedObject, IDistributedType
    {
        /// <summary>
        /// The host that contains this object.
        /// </summary>
        public DistributedHost Host { get; private set; }

        /// <summary>
        /// The NetPeer which owns this proxy, if this is a proxy.
        /// </summary>
        public NetPeer OwningPeer { get; private set; }

        /// <summary>
        /// Is this object the original, owner instance?
        /// </summary>
        /// <remarks>
        /// Owner objects relay commands to proxies, along with updating local state;
        /// proxy objects relay command requests to owners, and update local state only on commands from owners.
        /// </remarks>
        public bool IsOwner => OwningPeer == null;

        /// <summary>
        /// The id of this object; unique within its owning DistributedPeer.
        /// </summary>
        public DistributedId Id { get; private set; }

        /// <summary>
        /// The local object which implements the local behavior of the distributed object.
        /// </summary>
        public ILocalObject LocalObject { get; private set; }

        /// <summary>
        /// Create an owner DistributedObject.
        /// </summary>
        protected DistributedObject(DistributedHost host, ILocalObject localObject)
        {
            Contract.Requires(host != null);
            Contract.Requires(localObject != null);

            Host = host;
            Id = host.NextOwnerId();
            LocalObject = localObject;
            // and connect the local object to us
            LocalObject.Initialize(this);
        }

        /// <summary>
        /// Create a proxy DistributedObject.
        /// </summary>
        protected DistributedObject(DistributedHost host, NetPeer netPeer, DistributedId id, ILocalObject localObject)
        {
            Contract.Requires(host != null);
            Contract.Requires(netPeer != null);
            Contract.Requires(localObject != null);

            Host = host;
            OwningPeer = netPeer;
            Id = id;
            LocalObject = localObject;
            LocalObject.Initialize(this);
        }

        /// <summary>
        /// Detach this object from its Host; this occurs when the owner or proxy is deleted (or the proxy gets disconnected
        /// from the host).
        /// </summary>
        public void OnDetach()
        {
            Contract.Requires(Host != null);

            Host = null;
        }

        public void OnDelete()
        {
            OnDetach();
        }

        /// <summary>
        /// Delete this DistributedObject.
        /// </summary>
        /// <remarks>
        /// If called on the owner object, this will delete it (and all its proxies).  If called on a
        /// proxy object, this will send a deletion request to the owner.
        /// </remarks>
        public void Delete()
        {
            Contract.Requires(Host != null);

            Host.Delete(this, SendDeleteMessage);
        }

        /// <summary>
        /// This acts as its own IDistributedType implementation.
        /// </summary>
        public IDistributedType DistributedType => this;

        /// <summary>
        /// Get an action that will send the right CreateMessage to create a proxy for this object.
        /// </summary>
        /// <remarks>
        /// The LiteNetLib serialization library does not support polymorphism except for toplevel packets
        /// being sent (e.g. the only dynamic type mapping is in the NetPacketProcessor which maps packets
        /// to subscription callbacks).  So we can't make a generic CreateMessage with polymorphic payload.
        /// Instead, when it's time to create a proxy, we get an Action which will send the right CreateMessage
        /// to create the right proxy.
        /// </remarks>
        protected abstract void SendCreateMessage(NetPeer netPeer);

        public void SendCreateMessageInternal(NetPeer netPeer)
        {
            SendCreateMessage(netPeer);
        }

        /// <summary>
        /// Send the appropriate kind of DeleteMessage for this type of object.
        /// </summary>
        protected abstract void SendDeleteMessage(NetPeer netPeer, bool isRequest);

        public void SendDeleteMessageInternal(NetPeer netPeer, bool isRequest)
        {
            SendDeleteMessage(netPeer, isRequest);
        }
    }

    /// <summary>
    /// More strongly typed base class, for convenience of derived classes.
    /// </summary>
    public abstract class DistributedObject<TLocalObject> : DistributedObject
        where TLocalObject : ILocalObject
    {
        /// <summary>
        /// The local object wrapped by this distributed object (be it owner or proxy).
        /// </summary>
        public readonly TLocalObject TypedLocalObject;

        protected DistributedObject(DistributedHost peer, TLocalObject localObject)
            : base(peer, localObject)
        {
            TypedLocalObject = localObject;

            // and NOW add us as an owner object. Otherwise TypedLocalObject is not initialized yet.
            Host.AddOwner(this);
        }

        protected DistributedObject(DistributedHost peer, NetPeer owningPeer, DistributedId id, TLocalObject localObject)
            : base(peer, owningPeer, id, localObject)
        {
            TypedLocalObject = localObject;
        }

        protected abstract override void SendCreateMessage(NetPeer netPeer);
        protected abstract override void SendDeleteMessage(NetPeer netPeer, bool isRequest);

        /// <summary>
        /// Route a reliable message as appropriate (either forwarding to all proxies if owner, or sending request to owner if proxy).
        /// </summary>
        /// <typeparam name="TMessage">The type of message.</typeparam>
        /// <param name="messageFunc">Create a message given the IsRequest value (true if proxy, false if owner).</param>
        /// <param name="localAction">Update the local object if this is the owner.</param>
        protected void RouteReliableMessage<TMessage>(Func<bool, TMessage> messageFunc)
            where TMessage : ReliableMessage, new()
        {
            if (IsOwner)
            {
                // This is the canonical implementation of all IDistributedInterface methods on a distributed type implementation:
                // send a reliable non-request message to all proxies...
                TMessage message = messageFunc(false);
                Host.SendToProxies(message);

                // ...and update the local object.
                message.Invoke(LocalObject);
            }
            else
            {
                // send reliable request to owner
                Host.SendReliableMessage(messageFunc(true), OwningPeer);
            }
        }

        /// <summary>
        /// Route a broadcast message.
        /// </summary>
        /// <typeparam name="TMessage">The type of message.</typeparam>
        /// <param name="messageFunc">Create a message given the IsRequest value (true if proxy, false if owner).</param>
        protected void RouteBroadcastMessage<TMessage>(TMessage message)
            where TMessage : BroadcastMessage, new()
        {
            Host.SendBroadcastMessage(message);
            // and update the local object because we don't expect to hear our own broadcast... TBD tho
            message.Invoke(LocalObject);
        }
    }
}
