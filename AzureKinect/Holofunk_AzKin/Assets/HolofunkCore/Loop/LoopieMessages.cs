﻿// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Loop
{
    public class LoopieMessages : Messages
    {
        public class Create : CreateMessage
        {
            public Loopie Loopie { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, Loopie loopie) : base(id) { Loopie = loopie; }
            public override string ToString() => $"{base.ToString()}{Loopie}";
        }

        public class SetMute : ReliableMessage
        {
            public bool IsMuted { get; set; }
            public SetMute() : base() { }
            public SetMute(DistributedId id, bool isRequest, bool isMuted) : base(id, isRequest) { IsMuted = isMuted; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetMute(IsMuted);
        }

        public class SetVolume : ReliableMessage
        {
            public float Volume { get; set; }
            public SetVolume() : base() { }
            public SetVolume(DistributedId id, bool isRequest, float volume) : base(id, isRequest) { Volume = volume; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetVolume(Volume);
        }

        public class SetViewpointPosition : ReliableMessage
        {
            public Vector3 ViewpointPosition { get; set; }
            public SetViewpointPosition() : base() { }
            public SetViewpointPosition(DistributedId id, bool isRequest, Vector3 viewpointPosition) : base(id, isRequest) { ViewpointPosition = viewpointPosition; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetViewpointPosition(ViewpointPosition);
        }

        public class FinishRecording : ReliableMessage
        {
            public FinishRecording() : base() { }
            public FinishRecording(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).FinishRecording();
        }

        public class SetCurrentAmplitude : BroadcastMessage
        {
            float min, avg, max;
            public SetCurrentAmplitude() : base() { }
            public SetCurrentAmplitude(DistributedId id, SerializedSocketAddress owner, float min, float avg, float max)
                : base(id, owner)
            {
                this.min = min;
                this.avg = avg;
                this.max = max;
            }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetCurrentAmplitude(min, avg, max);
        }

        // TODO: refactor this for actual sharing with the other Register methods
        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedLoopie, LocalLoopie, IDistributedLoopie>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Loopie,
                (local, message) => local.Initialize(message.Loopie));
            Registrar.RegisterReliableMessage<SetMute, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<SetVolume, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<SetViewpointPosition, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<FinishRecording, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterBroadcastMessage<SetCurrentAmplitude, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
        }
    }
}
