// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using LiteNetLib.Utils;

namespace Holofunk.Hand
{
    /// <summary>
    /// What overall shape do we think the hand is in?
    /// </summary>
    /// <remarks>
    /// This list of poses is heavily informed by what is easy to recognize with some trivial linear
    /// algebra, intersecting with what the HL2 can reliably detect.
    /// </remarks>
    public enum HandPoseValue
    {
        /// <summary>
        /// No particular idea what shape the hand is in.
        /// </summary>
        Unknown,

        /// <summary>
        /// Pretty sure hand is open with all fingers extended and separated.
        /// </summary>
        Opened,
        
        /// <summary>
        /// Pretty sure hand is closed more or less into a fist.
        /// </summary>
        /// <remarks>
        /// If the hand is closed into a fist with fingers on the other side of the hand from the device, the device
        /// is prone to guess that the occluded fingers are extended. So we determine whether the finger vertices are
        /// colinear with a vector from the eye to the knuckle; if so, they are on the other side of the palm and we
        /// err on the side of assuming the hand is closed.
        /// </remarks>
        Closed,

        /// <summary>
        /// Pretty sure the hand is pointing with index finger only.
        /// </summary>
        PointingIndex,

        /// <summary>
        /// Pretty sure the hand is pointing with middle finger only.
        /// </summary>
        /// <remarks>
        /// This is likely enough to be a rude gesture that if the user does this a lot, they should
        /// be warned to cut it out.
        /// </remarks>
        PointingMiddle,

        /// <summary>
        /// Pretty sure hand is pointing with index and middle fingers adjacent.
        /// </summary>
        /// <remarks>
        /// Note that HL2 gets very unreliable at seeing the ring and pinky fingers precisely, for example
        /// it can't reliably see pointing with index, middle, and ring, and nor can it see the Vulcan greeting
        /// gesture.
        /// </remarks>
        PointingIndexAndMiddle,

        /// <summary>
        /// Bringing all fingertips together.
        /// </summary>
        Bloom,

        /// <summary>
        /// Pretty sure hand is fully flat with all fingers extended and adjacent.
        /// </summary>
        Flat,

        /// <summary>
        /// Thumbs up!
        /// </summary>
        ThumbsUp,

        /// <summary>
        /// Maximum value.
        /// </summary>
        Max = ThumbsUp
    }

    /// <summary>
    /// Serializable wrapper around HandPoseValue.
    /// </summary>
    public struct HandPose
    {
        private byte value;

        public HandPose(HandPoseValue value)
        {
            // ID 0 is not valid, reserved for uninitialized value
            Contract.Requires(value >= HandPoseValue.Unknown);
            Contract.Requires(value <= HandPoseValue.ThumbsUp);

            this.value = (byte)value;
        }

        public bool IsInitialized => value > (byte)HandPoseValue.Unknown;

        public static implicit operator HandPoseValue(HandPose pose) => (HandPoseValue)pose.value;

        public override string ToString() => $"#{(HandPoseValue)value}";

        public static bool operator ==(HandPose left, HandPose right) => left.Equals(right);

        public static bool operator !=(HandPose left, HandPose right) => !(left == right);

        public static void Serialize(NetDataWriter writer, HandPose pose) => writer.Put(pose.value);

        public static HandPose Deserialize(NetDataReader reader) => new HandPose((HandPoseValue)reader.GetByte());

        public override bool Equals(object obj) => obj is HandPose id && value == id.value;

        public override int GetHashCode() => -1584136870 + value.GetHashCode();
    }
}