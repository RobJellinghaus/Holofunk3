// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;

namespace Holofunk.VolumeWidget
{
    public class VolumeWidgetMessages : Messages
    {
        public class Create : CreateMessage
        {
            public VolumeWidgetState State { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, VolumeWidgetState state) : base(id) { State = state; }
        }

        public class UpdateState : ReliableMessage
        {
            public VolumeWidgetState State { get; set; }
            public UpdateState() : base() { }
            public UpdateState(DistributedId id, bool isRequest, VolumeWidgetState state) : base(id, isRequest) { State = state; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedVolumeWidget)target).UpdateState(State);
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType<VolumeWidgetState>();
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedVolumeWidget, LocalVolumeWidget, IDistributedVolumeWidget>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Viewpoint,
                (local, message) => local.Initialize(message.State));

            Registrar.RegisterReliableMessage<UpdateState, DistributedVolumeWidget, LocalVolumeWidget, IDistributedVolumeWidget>(
                proxyCapability);
        }
    }
}
