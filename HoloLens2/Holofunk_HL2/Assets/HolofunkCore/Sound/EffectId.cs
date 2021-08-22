// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;
using NowSoundLib;
using System;
using System.Collections.Generic;

namespace Holofunk.Sound
{
    /// <summary>
    /// A composite ID consisting of a plugin ID, and a program ID within that plugin.
    /// </summary>
    /// <remarks>
    /// Wraps the NowSoundLib PluginId type.
    /// </remarks>
    public struct EffectId
    {
        private PluginId pluginId;
        private PluginProgramId pluginProgramId;

        public EffectId(PluginId pluginId, PluginProgramId pluginProgramId)
        {
            Core.Contract.Assert(pluginId.IsInitialized);
            Core.Contract.Assert(pluginProgramId.IsInitialized);

            this.pluginId = pluginId;
            this.pluginProgramId = pluginProgramId;
        }

        public bool IsInitialized => pluginId.IsInitialized && pluginProgramId.IsInitialized;

        public PluginId PluginId => pluginId;
        public PluginProgramId PluginProgramId => pluginProgramId;

        public override string ToString()
        {
            return $"#{pluginId}.{pluginProgramId}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, EffectId effectId)
        {
            PluginId.Serialize(writer, effectId.pluginId);
            PluginProgramId.Serialize(writer, effectId.pluginProgramId);
        }

        public static EffectId Deserialize(NetDataReader reader) => new EffectId(
            PluginId.Deserialize(reader),
            PluginProgramId.Deserialize(reader));

        public override bool Equals(object obj)
        {
            return obj is EffectId id &&
                   EqualityComparer<PluginId>.Default.Equals(pluginId, id.pluginId) &&
                   EqualityComparer<PluginProgramId>.Default.Equals(pluginProgramId, id.pluginProgramId);
        }

        public override int GetHashCode()
        {
            int hashCode = 424028598;
            hashCode = hashCode * -1521134295 + pluginId.GetHashCode();
            hashCode = hashCode * -1521134295 + pluginProgramId.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(EffectId left, EffectId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EffectId left, EffectId right)
        {
            return !(left == right);
        }

    }
}