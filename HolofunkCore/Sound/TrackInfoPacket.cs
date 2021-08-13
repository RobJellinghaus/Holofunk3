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
    public struct TrackInfoPacket
    {
        private TrackInfo value;

        public TrackInfoPacket(TrackInfo value)
        {
            this.value = value;
        }

        public bool IsInitialized => value.StartTime != 0;

        public static implicit operator TrackInfoPacket(TrackInfo value) => new TrackInfoPacket(value);

        public TrackInfo Value => value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, TrackInfoPacket trackInfoPacket)
        {
            writer.Put(trackInfoPacket.Value.Duration);
            writer.Put(trackInfoPacket.Value.DurationInBeats);
            writer.Put((float)trackInfoPacket.Value.ExactDuration);
            writer.Put(trackInfoPacket.Value.IsTrackLooping);
            writer.Put((long)trackInfoPacket.Value.LastSampleTime);
            writer.Put((float)trackInfoPacket.Value.LocalClockBeat);
            writer.Put(trackInfoPacket.Value.LocalClockTime);
            writer.Put(trackInfoPacket.Value.Pan);
            writer.Put((long)trackInfoPacket.Value.StartTime);
            writer.Put((float)trackInfoPacket.Value.StartTimeInBeats);
        }

        public static TrackInfoPacket Deserialize(NetDataReader reader)
        {
            TrackInfo trackInfo = new TrackInfo(
                duration: reader.GetLong(),
                durationInBeats: reader.GetLong(),
                exactDuration: reader.GetFloat(),
                isTrackLooping: reader.GetBool(),
                lastSampleTime: reader.GetLong(),
                localClockBeat: reader.GetFloat(),
                localClockTime: reader.GetLong(),
                pan: reader.GetFloat(),
                startTime: reader.GetLong(),
                startTimeInBeats: reader.GetFloat());

            return new TrackInfoPacket(trackInfo);
        }

        public static bool operator ==(TrackInfoPacket left, TrackInfoPacket right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TrackInfoPacket left, TrackInfoPacket right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is TrackInfoPacket id
                   && Value.Duration == id.Value.Duration
                   && Value.DurationInBeats == id.Value.DurationInBeats
                   && (float)Value.ExactDuration == (float)id.Value.ExactDuration // note: loses units :-P TODO: proper ContinuousDuration equality
                   && Value.IsTrackLooping == id.Value.IsTrackLooping
                   && Value.LastSampleTime == id.Value.LastSampleTime
                   && (float)Value.LocalClockBeat == (float)id.Value.LocalClockBeat
                   && Value.LocalClockTime == id.Value.LocalClockTime
                   && Value.Pan == id.Value.Pan
                   && Value.StartTime == id.Value.StartTime
                   && (float)Value.StartTimeInBeats == (float)id.Value.StartTimeInBeats;                    
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}