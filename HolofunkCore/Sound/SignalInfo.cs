// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;
using NowSoundLib;
using System;

namespace Holofunk.Sound
{
    /// <summary>
    /// Information about an audio signal, for amplitude/rendering purposes.
    /// </summary>
    /// <remarks>
    /// Wraps the NowSoundLib SignalInfo type.
    /// </remarks>
    public struct SignalInfo
    {
        private NowSoundSignalInfo value;

        public SignalInfo(NowSoundSignalInfo value)
        {
            this.value = value;
        }

        public static implicit operator SignalInfo(NowSoundSignalInfo value) => new SignalInfo(value);

        public NowSoundSignalInfo Value => value;

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, SignalInfo signalInfoPacket)
        {
            writer.Put(signalInfoPacket.Value.Avg);
            writer.Put(signalInfoPacket.Value.Max);
            writer.Put(signalInfoPacket.Value.Min);
        }

        public static SignalInfo Deserialize(NetDataReader reader)
        {
            NowSoundSignalInfo signalInfo = new NowSoundSignalInfo()
            {
                Avg = reader.GetFloat(),
                Max = reader.GetFloat(),
                Min = reader.GetFloat()
            };

            return new SignalInfo(signalInfo);
        }

        public static bool operator ==(SignalInfo left, SignalInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SignalInfo left, SignalInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is SignalInfo id
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