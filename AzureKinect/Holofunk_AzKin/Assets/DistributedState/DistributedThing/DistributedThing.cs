// Copyright (c) 2020 by Rob Jellinghaus.
using Distributed.State;
using LiteNetLib;
using System.Collections.Generic;
using System.Linq;

namespace Distributed.Thing
{
    /// <summary>
    /// Distributed implementation of an IThing.
    /// </summary>
    /// <remarks>
    /// A DistributedObject implementation can handle a method call either reliably or unreliably.
    /// 
    /// Reliable method calls are routed to the owner (if this is a proxy), or sent to all proxies
    /// (if this is the owner). Local objects are updated only by the owner or by a proxy message
    /// from the owner.
    /// 
    /// Unreliable method calls result in a broadcast to all other instances of this object.
    /// </remarks>
    public class DistributedThing : DistributedObject<LocalThing>, IThing
    {
        public DistributedThing(DistributedHost peer, LocalThing localThing)
            : base(peer, localThing)
        {
        }

        public DistributedThing(DistributedHost peer, NetPeer owningPeer, int id, LocalThing localThing)
            : base(peer, owningPeer, id, localThing)
        {
        }

        public IEnumerable<int> LocalValues => TypedLocalObject?.LocalValues ?? Enumerable.Empty<int>();

        /// <summary>
        /// Handle an Enqueue message by using default routing.
        /// </summary>
        public void Enqueue(int[] values)
        {
            RouteReliableMessage(isRequest => new ThingMessages.Enqueue(Id, isRequest, values));
        }

        public void Ping(char[] message)
        {
            RouteBroadcastMessage(new ThingMessages.Ping(Id, new SerializedSocketAddress(OwningPeer), message));
        }

        public char[] LastMessage => TypedLocalObject?.LastMessage ?? null;

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            Host.SendReliableMessage(new ThingMessages.Create(Id, LocalValues.ToArray()), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            Host.SendReliableMessage(new ThingMessages.Delete(Id, isRequest), netPeer);
        }
    }
}
