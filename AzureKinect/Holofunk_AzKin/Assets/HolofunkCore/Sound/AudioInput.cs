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
    public struct AudioInput
    {
        private AudioInputId value;

        public AudioInput(AudioInputId value)
        {
            this.value = value;
        }

        public bool IsInitialized => value != AudioInputId.AudioInputUndefined;

        public static implicit operator AudioInput(AudioInputId value) => new AudioInput(value);

        public AudioInputId Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, AudioInput audioInput) => writer.Put((int)audioInput.Value);

        public static AudioInput Deserialize(NetDataReader reader) => new AudioInput((AudioInputId)reader.GetInt());

        public static bool operator ==(AudioInput left, AudioInput right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AudioInput left, AudioInput right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is AudioInput id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}