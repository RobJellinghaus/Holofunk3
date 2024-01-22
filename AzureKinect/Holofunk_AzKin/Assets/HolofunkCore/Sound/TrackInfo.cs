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
            writer.Put(trackInfo.Value.IsTrackLooping);
            writer.Put(trackInfo.Value.BeatDuration);
            writer.Put((float)trackInfo.Value.ExactDuration);
            writer.Put((float)trackInfo.Value.ExactTrackTime);
            writer.Put((float)trackInfo.Value.ExactTrackBeat);
            writer.Put(trackInfo.Value.Pan);
            writer.Put(trackInfo.Value.Volume);
            writer.Put(trackInfo.Value.BeatsPerMinute);
            writer.Put((int)trackInfo.Value.BeatsPerMeasure);
        }

        public static TrackInfo Deserialize(NetDataReader reader)
        {
            NowSoundLib.TrackInfo trackInfo = new NowSoundLib.TrackInfo(
                isTrackLooping: reader.GetBool(),
                beatDuration: reader.GetLong(),
                exactDuration: reader.GetFloat(),
                exactTrackTime: reader.GetFloat(),
                exactTrackBeat: reader.GetFloat(),
                pan: reader.GetFloat(),
                volume: reader.GetFloat(),
                beatsPerMinute: reader.GetFloat(),
                beatsPerMeasure: reader.GetInt());

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
                   && Value.IsTrackLooping == id.Value.IsTrackLooping
                   && Value.BeatDuration == id.Value.BeatDuration
                   && (float)Value.ExactDuration == (float)id.Value.ExactDuration // note: loses units :-P TODO: proper ContinuousDuration equality
                   && (float)Value.ExactTrackTime == (float)id.Value.ExactTrackTime
                   && (float)Value.ExactTrackBeat == (float)id.Value.ExactTrackBeat
                   && Value.Pan == id.Value.Pan
                   && Value.Volume == id.Value.Volume
                   && (float)Value.BeatsPerMinute == (float)id.Value.BeatsPerMinute
                   && Value.BeatsPerMeasure == id.Value.BeatsPerMeasure;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}