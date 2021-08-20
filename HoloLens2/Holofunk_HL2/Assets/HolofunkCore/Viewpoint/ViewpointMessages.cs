// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;

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

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType(PlayerId.Serialize, PlayerId.Deserialize);
            proxyCapability.RegisterType(UserId.Serialize, UserId.Deserialize);
            proxyCapability.RegisterType<PlayerState>();
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Viewpoint,
                (local, message) => local.Initialize(message.Players));

            Registrar.RegisterReliableMessage<UpdatePlayer, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                proxyCapability);
        }
    }
}
