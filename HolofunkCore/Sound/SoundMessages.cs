// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib;
using UnityEngine;

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

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType(AudioInputId.Serialize, AudioInputId.Deserialize);
            proxyCapability.RegisterType(PluginId.Serialize, PluginId.Deserialize);
            proxyCapability.RegisterType(PluginProgramId.Serialize, PluginProgramId.Deserialize);

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
        }
    }
}
