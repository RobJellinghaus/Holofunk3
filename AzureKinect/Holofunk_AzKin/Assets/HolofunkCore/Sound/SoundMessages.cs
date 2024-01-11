// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Distributed;
using Holofunk.Sound;

namespace Holofunk.Viewpoint
{
    public class SoundMessages : Messages
    {
        /// <summary>
        /// Create a SoundEffect.
        /// </summary>
        /// <remarks>
        /// TODO: consider some generic state abstraction to make all this create plumbing be shared.
        /// </remarks>
        public class CreateSoundEffect : CreateMessage
        {
            public PluginId PluginId { get; set; }
            public string PluginName { get; set; }
            public PluginProgramId PluginProgramId { get; set; }
            public string ProgramName { get; set; }
            public CreateSoundEffect() : base() { }
            public CreateSoundEffect(DistributedId id, PluginId pluginId, string pluginName, PluginProgramId pluginProgramId, string programName)
                : base(id)
            {
                PluginId = pluginId;
                PluginName = pluginName;
                PluginProgramId = pluginProgramId;
                ProgramName = programName;
            }
        }

        /// <summary>
        /// Create a SoundClock.
        /// </summary>
        /// <remarks>
        /// TODO: consider some generic state abstraction to make all this create plumbing be shared.
        /// </remarks>
        public class CreateSoundClock : CreateMessage
        {
            public TimeInfo TimeInfo { get; set; }
            public CreateSoundClock() : base() { }
            public CreateSoundClock(DistributedId id, TimeInfo timeInfo)
                : base(id)
            {
                TimeInfo = timeInfo;
            }
        }

        /// <summary>
        /// Update a SoundClock's TimeInfo.
        /// </summary>
        /// <remarks>
        /// TODO: consider some generic state abstraction to make all this create plumbing be shared.
        /// </remarks>
        public class UpdateSoundClockTimeInfo : ReliableMessage
        {
            public TimeInfo TimeInfo { get; set; }
            public UpdateSoundClockTimeInfo() : base() { }
            public UpdateSoundClockTimeInfo(DistributedId id, bool isRequest, TimeInfo timeInfo) : base(id, isRequest) { TimeInfo = timeInfo; }
            public override void Invoke(IDistributedInterface target) => ((IDistributedSoundClock)target).UpdateTimeInfo(TimeInfo);
        }

        /// <summary>
        /// Set a SoundClock's tempo.
        /// </summary>
        /// <remarks>
        /// Note that this does not alter the tempo of existing Loopies at all. They keep their own tempo. Let chaos reign!
        /// </remarks>
        public class UpdateSoundClockTempo : ReliableMessage
        {
            public float BeatsPerMinute { get; set; }
            public int BeatsPerMeasure { get; set; }
            public UpdateSoundClockTempo() : base() { }
            public UpdateSoundClockTempo(DistributedId id, bool isRequest, float beatsPerMinute, int beatsPerMeasure) 
                : base(id, isRequest) 
            { 
                BeatsPerMinute = beatsPerMinute; 
                BeatsPerMeasure = beatsPerMeasure;
            }
            public override void Invoke(IDistributedInterface target) => ((IDistributedSoundClock)target).SetTempo(BeatsPerMinute, BeatsPerMeasure);
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType(AudioInputId.Serialize, AudioInputId.Deserialize);
            proxyCapability.RegisterType(PluginId.Serialize, PluginId.Deserialize);
            proxyCapability.RegisterType(PluginProgramId.Serialize, PluginProgramId.Deserialize);
            proxyCapability.RegisterType(EffectId.Serialize, EffectId.Deserialize);

            proxyCapability.RegisterType(SignalInfo.Serialize, SignalInfo.Deserialize);
            proxyCapability.RegisterType(TimeInfo.Serialize, TimeInfo.Deserialize);
            proxyCapability.RegisterType(TrackInfo.Serialize, TrackInfo.Deserialize);
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<CreateSoundEffect, DistributedSoundEffect, LocalSoundEffect, IDistributedSoundEffect>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.SoundEffect,
                (local, message) => local.Initialize(message.PluginId, message.PluginName, message.PluginProgramId, message.ProgramName));

            Registrar.RegisterCreateMessage<CreateSoundClock, DistributedSoundClock, LocalSoundClock, IDistributedSoundClock>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.SoundClock,
                (local, message) => local.Initialize(message.TimeInfo));

            Registrar.RegisterReliableMessage<UpdateSoundClockTimeInfo, DistributedSoundClock, LocalSoundClock, IDistributedSoundClock>(
                proxyCapability);

            Registrar.RegisterReliableMessage<UpdateSoundClockTempo, DistributedSoundClock, LocalSoundClock, IDistributedSoundClock>(
                proxyCapability);
        }
    }
}
