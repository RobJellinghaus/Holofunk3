// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Loop
{
    public class LoopieMessages : Messages
    {
        public class Create : CreateMessage
        {
            public LoopieState Loopie { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, LoopieState loopie) : base(id) { Loopie = loopie; }
            public override string ToString() => $"{base.ToString()}{Loopie}";
        }

        public class Delete : DeleteMessage
        {
            public Delete() : base() { }
            public Delete(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override string ToString() => $"{base.ToString()}{Id}";
        }

        public class SetMute : ReliableMessage
        {
            public bool IsMuted { get; set; }
            public SetMute() : base() { }
            public SetMute(DistributedId id, bool isRequest, bool isMuted) : base(id, isRequest) { IsMuted = isMuted; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetMute(IsMuted);
        }

        public class MultiplyVolume : ReliableMessage
        {
            public float Ratio { get; set; }
            public MultiplyVolume() : base() { }
            public MultiplyVolume(DistributedId id, bool isRequest, float ratio) : base(id, isRequest) { Ratio = ratio; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).MultiplyVolume(Ratio);
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

        public class AppendSoundEffect : ReliableMessage
        {
            public EffectId Effect { get; set; }
            public AppendSoundEffect() : base() { }
            public AppendSoundEffect(DistributedId id, bool isRequest, EffectId effect) : base(id, isRequest) { Effect = effect; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).AppendSoundEffect(Effect);
        }

        public class ClearSoundEffects : ReliableMessage
        {
            public ClearSoundEffects() : base() { }
            public ClearSoundEffects(DistributedId id, bool isRequest) : base(id, isRequest) { }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).ClearSoundEffects();
        }

        public class SetCurrentInfo : BroadcastMessage
        {
            public SignalInfo SignalInfo { get; set; }
            public TrackInfo TrackInfo { get; set; }
            public ulong Timestamp { get; set; }
            public SetCurrentInfo() : base() { }
            public SetCurrentInfo(DistributedId id, SerializedSocketAddress owner, SignalInfo signalInfo, TrackInfo trackInfo, ulong timestamp)
                : base(id, owner)
            {
                SignalInfo = signalInfo;
                TrackInfo = trackInfo;
                Timestamp = timestamp;
            }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetCurrentInfo(SignalInfo, TrackInfo, Timestamp);
        }

        public class SetCurrentWaveform : BroadcastMessage
        {
            public float[] FrequencyBins { get; set; }
            public ulong Timestamp { get; set; }
            public SetCurrentWaveform() : base() { }
            public SetCurrentWaveform(DistributedId id, SerializedSocketAddress owner, float[] frequencyBins, ulong timestamp)
                : base(id, owner)
            {
                FrequencyBins = frequencyBins;
                Timestamp = timestamp;
            }
            public override void Invoke(IDistributedInterface target) => ((IDistributedLoopie)target).SetCurrentWaveform(FrequencyBins, Timestamp);
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType<LoopieState>();
        }

        // TODO: refactor this for actual sharing with the other Register methods
        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedLoopie, LocalLoopie, IDistributedLoopie>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Loopie,
                (local, message) => local.Initialize(message.Loopie));
            Registrar.RegisterDeleteMessage<Delete, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<SetMute, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<MultiplyVolume, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<SetViewpointPosition, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<FinishRecording, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<AppendSoundEffect, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterReliableMessage<ClearSoundEffects, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterBroadcastMessage<SetCurrentInfo, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
            Registrar.RegisterBroadcastMessage<SetCurrentWaveform, DistributedLoopie, LocalLoopie, IDistributedLoopie>(proxyCapability);
        }
    }
}
