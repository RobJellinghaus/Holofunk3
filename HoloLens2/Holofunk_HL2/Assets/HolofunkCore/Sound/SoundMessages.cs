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
        public class Create : CreateMessage
        {
            public int PluginId { get; set; }
            public string PluginName { get; set; }
            public int PluginProgramId { get; set; }
            public string ProgramName { get; set; }
            public Create() : base() { }
            public Create(DistributedId id, int pluginId, string pluginName, int pluginProgramId, string programName)
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
            proxyCapability.RegisterType(AudioInput.Serialize, AudioInput.Deserialize);
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
