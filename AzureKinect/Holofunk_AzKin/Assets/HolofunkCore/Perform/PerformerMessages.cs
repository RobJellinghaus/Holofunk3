// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Perform
{
    public class PerformerMessages : Messages
    {
        public class Create : CreateMessage
        {
            public Performer Performer { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, Performer performer) : base(id)
            {
                Performer = performer;
            }
        }

        public class UpdatePerformer : ReliableMessage
        {
            public Performer Performer { get; set; }
            public UpdatePerformer() : base() { }

            public UpdatePerformer(DistributedId id, bool isRequest, Performer performer) : base(id, isRequest)
            {
                Performer = performer;
            }

            public override void Invoke(IDistributedInterface target)
            {
                ((IDistributedPerformer)target).UpdatePerformer(Performer);
            }
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.SubscribeReusable((Create createMessage, NetPeer netPeer) =>
            {
                // get the prototype object
                GameObject prototype = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Performer);
                GameObject parent = DistributedObjectFactory.FindContainer(DistributedObjectFactory.DistributedType.Performer, netPeer);
                GameObject clone = UnityEngine.Object.Instantiate(prototype, parent.transform);

                clone.name = $"{createMessage.Id}";

                HoloDebug.Log($"Received PerformerMessages.Create for id {createMessage.Id} from peer {netPeer.EndPoint}");

                // wire the local and distributed things together
                LocalPerformer local = clone.GetComponent<LocalPerformer>();
                DistributedPerformer distributed = clone.GetComponent<DistributedPerformer>();

                local.Initialize(createMessage.Performer);
                distributed.InitializeProxy(netPeer, createMessage.Id);

                proxyCapability.AddProxy(netPeer, distributed);
            });

            proxyCapability.SubscribeReusable((UpdatePerformer updatePerformerMessage, NetPeer netPeer) =>
                HandleReliableMessage<UpdatePerformer, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                    proxyCapability.Host,
                    netPeer,
                    updatePerformerMessage));
        }
    }
}
