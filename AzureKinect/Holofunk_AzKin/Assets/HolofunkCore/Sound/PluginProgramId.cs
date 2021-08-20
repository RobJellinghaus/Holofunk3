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
    public struct PluginProgramId
    {
        private ProgramId value;

        public PluginProgramId(ProgramId value)
        {
            this.value = value;
        }

        public bool IsInitialized => value != ProgramId.Undefined;

        public ProgramId Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, PluginProgramId pluginId) => writer.Put((int)pluginId.Value);

        public static PluginProgramId Deserialize(NetDataReader reader) => new PluginProgramId((ProgramId)reader.GetInt());

        public static bool operator ==(PluginProgramId left, PluginProgramId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PluginProgramId left, PluginProgramId right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is PluginProgramId id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}