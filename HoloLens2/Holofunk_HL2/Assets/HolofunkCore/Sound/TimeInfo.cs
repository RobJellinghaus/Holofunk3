// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;
using NowSoundLib;

namespace Holofunk.Sound
{
    /// <summary>
    /// Information about an audio track, serializably.
    /// </summary>
    /// <remarks>
    /// Wraps the NowSoundLib TimeInfo type.
    /// </remarks>
    public struct TimeInfo
    {
        private NowSoundLib.TimeInfo value;

        public TimeInfo(NowSoundLib.TimeInfo value)
        {
            this.value = value;
        }

        public bool IsInitialized => value.TimeInSamples != 0;

        public static implicit operator TimeInfo(NowSoundLib.TimeInfo value) => new TimeInfo(value);

        public NowSoundLib.TimeInfo Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, TimeInfo TimeInfo)
        {
            writer.Put(TimeInfo.Value.BeatInMeasure);
            writer.Put(TimeInfo.Value.BeatsPerMinute);
            writer.Put((float)TimeInfo.Value.ExactBeat);
            writer.Put((long)TimeInfo.Value.TimeInSamples);
        }

        public static TimeInfo Deserialize(NetDataReader reader)
        {
            NowSoundLib.TimeInfo TimeInfo = new NowSoundLib.TimeInfo(
                beatsInMeasure: reader.GetFloat(),
                beatsPerMinute: reader.GetFloat(),
                exactBeat: reader.GetFloat(),
                timeInSamples: reader.GetLong());

            return new TimeInfo(TimeInfo);
        }

        public static bool operator ==(TimeInfo left, TimeInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimeInfo left, TimeInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is TimeInfo id
                   && Value.BeatInMeasure == id.Value.BeatInMeasure
                   && Value.BeatsPerMinute == id.Value.BeatsPerMinute
                   && (float)Value.ExactBeat == (float)id.Value.ExactBeat // note: loses units :-P TODO: proper ContinuousDuration equality
                   && Value.TimeInSamples == id.Value.TimeInSamples;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}