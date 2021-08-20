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
        public class Create : CreateMessage
        {
            public PluginId PluginId { get; set; }
            public string PluginName { get; set; }
            public PluginProgramId PluginProgramId { get; set; }
            public string ProgramName { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, PluginId pluginId, string pluginName, PluginProgramId pluginProgramId, string programName)
                : base(id)
            {
                PluginId = pluginId;
                PluginName = pluginName;
                PluginProgramId = pluginProgramId;
                ProgramName = programName;
            }
        }

        public static void RegisterTypes(DistributedHost.ProxyCapability proxyCapability)
        {
            proxyCapability.RegisterType(AudioInputId.Serialize, AudioInputId.Deserialize);
            proxyCapability.RegisterType(SignalInfoPacket.Serialize, SignalInfoPacket.Deserialize);
            proxyCapability.RegisterType(TrackInfoPacket.Serialize, TrackInfoPacket.Deserialize);
        }

        public static void Register(DistributedHost.ProxyCapability proxyCapability)
        {
            Registrar.RegisterCreateMessage<Create, DistributedSoundEffect, LocalSoundEffect, IDistributedSoundEffect>(
                proxyCapability,
                DistributedObjectFactory.DistributedType.Viewpoint,
                (local, message) => local.Initialize(message.PluginId, message.PluginName, message.PluginProgramId, message.ProgramName));
        }
    }
}
