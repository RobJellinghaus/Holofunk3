// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;
using NowSoundLib;

namespace Holofunk.Sound
{
    /// <summary>
    /// Information about an audio track, serializably.
    /// </summary>
    /// <remarks>
    /// Wraps the NowSoundLib TrackInfo type.
    /// </remarks>
    public struct TrackInfo
    {
        private NowSoundLib.TrackInfo value;

        public TrackInfo(NowSoundLib.TrackInfo value)
        {
            this.value = value;
        }

        public bool IsInitialized => value.StartTime != 0;

        public static implicit operator TrackInfo(NowSoundLib.TrackInfo value) => new TrackInfo(value);

        public NowSoundLib.TrackInfo Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, TrackInfo trackInfo)
        {
            writer.Put(trackInfo.Value.Duration);
            writer.Put(trackInfo.Value.DurationInBeats);
            writer.Put((float)trackInfo.Value.ExactDuration);
            writer.Put(trackInfo.Value.IsTrackLooping);
            writer.Put((long)trackInfo.Value.LastSampleTime);
            writer.Put((float)trackInfo.Value.LocalClockBeat);
            writer.Put(trackInfo.Value.LocalClockTime);
            writer.Put(trackInfo.Value.Pan);
            writer.Put((long)trackInfo.Value.StartTime);
            writer.Put((float)trackInfo.Value.StartTimeInBeats);
            writer.Put(trackInfo.Value.Volume);
        }

        public static TrackInfo Deserialize(NetDataReader reader)
        {
            NowSoundLib.TrackInfo trackInfo = new NowSoundLib.TrackInfo(
                duration: reader.GetLong(),
                durationInBeats: reader.GetLong(),
                exactDuration: reader.GetFloat(),
                isTrackLooping: reader.GetBool(),
                lastSampleTime: reader.GetLong(),
                localClockBeat: reader.GetFloat(),
                localClockTime: reader.GetLong(),
                pan: reader.GetFloat(),
                startTime: reader.GetLong(),
                startTimeInBeats: reader.GetFloat(),
                volume: reader.GetFloat());

            return new TrackInfo(trackInfo);
        }

        public static bool operator ==(TrackInfo left, TrackInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TrackInfo left, TrackInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is TrackInfo id
                   && Value.Duration == id.Value.Duration
                   && Value.DurationInBeats == id.Value.DurationInBeats
                   && (float)Value.ExactDuration == (float)id.Value.ExactDuration // note: loses units :-P TODO: proper ContinuousDuration equality
                   && Value.IsTrackLooping == id.Value.IsTrackLooping
                   && Value.LastSampleTime == id.Value.LastSampleTime
                   && (float)Value.LocalClockBeat == (float)id.Value.LocalClockBeat
                   && Value.LocalClockTime == id.Value.LocalClockTime
                   && Value.Pan == id.Value.Pan
                   && Value.StartTime == id.Value.StartTime
                   && (float)Value.StartTimeInBeats == (float)id.Value.StartTimeInBeats
                   && Value.Volume == id.Value.Volume;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}