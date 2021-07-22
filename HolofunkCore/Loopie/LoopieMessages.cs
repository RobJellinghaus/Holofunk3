// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Loopie
{
    public class LoopieMessages : Messages
    {
        public class Create : CreateMessage
        {
            public Loopie Loopie{ get; set; }
            public Create() : base() { }
            public Create(DistributedId id, Loopie loopie) : base(id)
            {
                Loopie = loopie;
            }
        }

        public class SetMute : ReliableMessage
        {
            public bool IsMuted { get; set; }
            public SetMute() : base() { }

            public SetMute(DistributedId id, bool isRequest, bool isMuted) : base(id, isRequest)
            {
                IsMuted = isMuted;
            }

            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetMute(IsMuted);
        }

        public class SetVolume : ReliableMessage
        {
            public float Volume { get; set; }
            public SetVolume() : base() { }

            public SetVolume(DistributedId id, bool isRequest, float volume) : base(id, isRequest)
            {
                Volume = volume;
            }

            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetVolume(Volume);
        }

        // TODO: refactor this for actual sharing with the other Register methods
        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedLoopie, LocalLoopie, IDistributedLoopie>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Loopie,
                (local, message) => local.Initialize(message.Loopie));

            Registrar.RegisterReliableMessage<SetMute, DistributedLoopie, LocalLoopie, IDistributedLoopie>(
                proxyCapability);

            Registrar.RegisterReliableMessage<SetVolume, DistributedLoopie, LocalLoopie, IDistributedLoopie>(
                proxyCapability);
        }
    }
}
