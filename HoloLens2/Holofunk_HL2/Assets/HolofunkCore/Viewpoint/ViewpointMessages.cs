// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;

namespace Holofunk.Viewpoint
{
    public class ViewpointMessages : Messages
    {
        public class Create : CreateMessage
        {
            public PlayerState[] Players { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, PlayerState[] players) : base(id) { Players = players; }
        }

        public class UpdatePlayer : ReliableMessage
        {
            public PlayerState Player { get; set; }
            public UpdatePlayer() : base() { }
            public UpdatePlayer(DistributedId id, bool isRequest, PlayerState player) : base(id, isRequest) { Player = player; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedViewpoint)target).UpdatePlayer(Player);
        }

        public class StartRecording : ReliableMessage
        {
            public StartRecording() : base() { }
            public StartRecording(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override void Invoke(IDistributedInterface target) => ((IDistributedViewpoint)target).StartRecording();
        }

        public class StopRecording : ReliableMessage
        {
            public StopRecording() : base() { }
            public StopRecording(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override void Invoke(IDistributedInterface target) => ((IDistributedViewpoint)target).StopRecording();
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType(PlayerId.Serialize, PlayerId.Deserialize);
            proxyCapability.RegisterType(UserId.Serialize, UserId.Deserialize);
            proxyCapability.RegisterType<PlayerState>();
            proxyCapability.RegisterType<ViewpointState>();
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Viewpoint,
                (local, message) => local.Initialize(message.Players));

            Registrar.RegisterReliableMessage<UpdatePlayer, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                proxyCapability);
            Registrar.RegisterReliableMessage<StartRecording, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                proxyCapability);
            Registrar.RegisterReliableMessage<StopRecording, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                proxyCapability);
        }
    }
}
