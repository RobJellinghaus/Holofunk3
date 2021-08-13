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
    /// Wraps the NowSoundLib TimeInfo type.
    /// </remarks>
    public struct SignalInfoPacket
    {
        private NowSoundSignalInfo value;

        public SignalInfoPacket(NowSoundSignalInfo value)
        {
            this.value = value;
        }

        public static implicit operator SignalInfoPacket(NowSoundSignalInfo value) => new SignalInfoPacket(value);

        public NowSoundSignalInfo Value => value;

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, SignalInfoPacket signalInfoPacket)
        {
            writer.Put(signalInfoPacket.Value.Avg);
            writer.Put(signalInfoPacket.Value.Max);
            writer.Put(signalInfoPacket.Value.Min);
        }

        public static SignalInfoPacket Deserialize(NetDataReader reader)
        {
            NowSoundSignalInfo signalInfo = new NowSoundSignalInfo()
            {
                Avg = reader.GetFloat(),
                Max = reader.GetFloat(),
                Min = reader.GetFloat()
            };

            return new SignalInfoPacket(signalInfo);
        }

        public static bool operator ==(SignalInfoPacket left, SignalInfoPacket right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SignalInfoPacket left, SignalInfoPacket right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is SignalInfoPacket id
                   && Value.Avg == id.Value.Avg
                   && Value.Max == id.Value.Max
                   && Value.Min == id.Value.Min;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}