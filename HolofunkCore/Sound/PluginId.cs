// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;
using NowSoundLib;
using System;

namespace Holofunk.Sound
{
    /// <summary>
    /// ID of an audio plugin, suitable for distribution.
    /// </summary>
    /// <remarks>
    /// Wraps the NowSoundLib PluginId type.
    /// </remarks>
    public struct PluginId
    {
        private NowSoundLib.PluginId value;

        public PluginId(NowSoundLib.PluginId value)
        {
            this.value = value;
        }

        public bool IsInitialized => value != NowSoundLib.PluginId.Undefined;

        public NowSoundLib.PluginId Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, PluginId pluginId) => writer.Put((int)pluginId.Value);

        public static PluginId Deserialize(NetDataReader reader) => new PluginId((NowSoundLib.PluginId)reader.GetInt());

        public static bool operator ==(PluginId left, PluginId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PluginId left, PluginId right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is PluginId id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}