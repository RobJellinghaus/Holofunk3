// Copyright (c) 2021 by Rob Jellinghaus.

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
            public Player[] Players { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, Player[] players) : base(id)
            {
                Players = players;
            }
        }

        public class UpdatePlayer : ReliableMessage
        {
            public Player Player { get; set; }
            public UpdatePlayer() : base() { }

            public UpdatePlayer(DistributedId id, bool isRequest, Player player) : base(id, isRequest)
            {
                Player = player;
            }

            public override void Invoke(IDistributedInterface target)
            {
                ((IDistributedViewpoint)target).UpdatePlayer(Player);
            }
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.SubscribeReusable((Create createMessage, NetPeer netPeer) =>
            {
                // get the prototype object
                GameObject prototype = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Viewpoint);
                GameObject parent = DistributedObjectFactory.FindContainer(DistributedObjectFactory.DistributedType.Viewpoint, netPeer);
                GameObject clone = UnityEngine.Object.Instantiate(prototype, parent.transform);

                clone.name = $"{createMessage.Id}";

                HoloDebug.Log($"Received ViewpointMessages.Create for id {createMessage.Id} from peer {netPeer.EndPoint}");

                // wire the local and distributed things together
                LocalViewpoint local = clone.GetComponent<LocalViewpoint>();
                DistributedViewpoint distributed = clone.GetComponent<DistributedViewpoint>();

                local.Initialize(createMessage.Players);
                distributed.InitializeProxy(netPeer, createMessage.Id);

                proxyCapability.AddProxy(netPeer, distributed);
            });

            proxyCapability.SubscribeReusable((UpdatePlayer updatePlayerMessage, NetPeer netPeer) =>
                HandleReliableMessage<UpdatePlayer, DistributedViewpoint, LocalViewpoint, IDistributedViewpoint>(
                    proxyCapability.Host,
                    netPeer,
                    updatePlayerMessage));
        }
    }
}
