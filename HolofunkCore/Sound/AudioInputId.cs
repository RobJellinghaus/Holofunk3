// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;
using NowSoundLib;
using System;

namespace Holofunk.Sound
{
    /// <summary>
    /// ID of an audio input, suitable for distribution.
    /// </summary>
    /// <remarks>
    /// Wraps the NowSoundLib AudioInputId type.
    /// </remarks>
    public struct AudioInputId
    {
        private NowSoundLib.AudioInputId value;

        public AudioInputId(NowSoundLib.AudioInputId value)
        {
            this.value = value;
        }

        public bool IsInitialized => value != NowSoundLib.AudioInputId.AudioInputUndefined;

        public NowSoundLib.AudioInputId Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, AudioInputId audioInput) => writer.Put((int)audioInput.Value);

        public static AudioInputId Deserialize(NetDataReader reader) => new AudioInputId((NowSoundLib.AudioInputId)reader.GetInt());

        public static bool operator ==(AudioInputId left, AudioInputId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AudioInputId left, AudioInputId right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is AudioInputId id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}