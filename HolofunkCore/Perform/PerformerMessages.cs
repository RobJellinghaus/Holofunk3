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
            public Create(DistributedId id, Performer performer) : base(id) { Performer = performer; }
        }

        public class UpdatePerformer : ReliableMessage
        {
            public Performer Performer { get; set; }
            public UpdatePerformer() : base() { }
            public UpdatePerformer(DistributedId id, bool isRequest, Performer performer) : base(id, isRequest) { Performer = performer; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedPerformer)target).UpdatePerformer(Performer);
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType<Performer>();
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Performer,
                (local, message) => local.Initialize(message.Performer));

            Registrar.RegisterReliableMessage<UpdatePerformer, DistributedPerformer, LocalPerformer, IDistributedPerformer>(
                proxyCapability);
        }
    }
}
