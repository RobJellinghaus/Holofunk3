/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Base component class implementing IDistributedObject.
    /// </summary>
    /// <remarks>
    /// Specific implementations of distributed objects will have their own Distributed*
    /// component classes deriving from this class.
    /// </remarks>
    public abstract class DistributedComponent : MonoBehaviour, IDistributedObject, IDistributedType
    {
        /// <summary>
        /// We always expect the DistributedHoster GameObject to be the very first child of the root GameObject.
        /// </summary>
        public DistributedHost Host => DistributedHoster.Host;

        public DistributedId Id { get; private set; }

        public NetPeer OwningPeer { get; private set; }

        public bool IsOwner => OwningPeer == null;

        public abstract ILocalObject LocalObject { get; }

        public abstract void Delete();

        public abstract void OnDelete();

        public abstract void OnDetach();

        /// <summary>
        /// This is an owner Component; initialize it as such.
        /// </summary>
        public void InitializeOwner()
        {
            // make sure object hasn't been initialized yet
            Contract.Requires(OwningPeer == null);
            Contract.Requires(!Id.IsInitialized);

            DistributedHost host = Host;
            Id = host.NextOwnerId();
            // OwningPeer remains, and will forever be, null.

            // Add this object as a known owner in the current distributed session.
            // This operation triggers proxy creation on other hosts.
            // RISKY OPERATION if InitializeOwner() is called during component construction.
            // In current model, this doesn't happen since Unity instantiates all Components,
            // and InitializeOwner() only gets called from Start() after construction is complete.
            host.AddOwner(this);

        }

        public void InitializeProxy(NetPeer owningPeer, DistributedId id)
        {
            // make sure args are valid
            Contract.Requires(owningPeer != null);
            Contract.Requires(id.IsInitialized);

            // make sure object hasn't been initialized yet
            Contract.Requires(OwningPeer == null);
            Contract.Requires(!Id.IsInitialized);

            OwningPeer = owningPeer;
            Id = id;
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
                TMessage message = messageFunc(/*isRequest:*/ false);
                Host.SendToProxies(message);

                // ...and update the local object.
                message.Invoke(LocalObject);
            }
            else
            {
                // send reliable request to owner
                Host.SendReliableMessage(messageFunc(/*isRequest:*/ true), OwningPeer);
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
