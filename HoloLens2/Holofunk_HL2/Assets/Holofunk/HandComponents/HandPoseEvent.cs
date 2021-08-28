/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Hand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holofunk.HandComponents
{
    /// <summary>An event in a Loopie machine.</summary>
    public struct HandPoseEvent
    {
        readonly HandPoseValue _value;
        readonly bool _isCaptured;

        /// <summary>The value of the hand pose.</summary>
        internal HandPoseValue Value { get { return _value; } }

        /// <summary>
        /// Has this event already been captured?
        /// </summary>
        /// <remarks>
        /// We may still want to respond to already-captured events in some way (e.g. by changing a hand sprite),
        /// but we want to be able to detect that the capture already took place, and modify the transition
        /// appropriately.
        /// </remarks>
        internal bool IsCaptured { get { return _isCaptured; } }

        internal HandPoseEvent(HandPoseValue type) { _value = type; _isCaptured = false; }

        /// <summary>
        /// Construct a captured BodyPoseEvent from an existing one.
        /// </summary>
        internal HandPoseEvent AsCaptured() { return new HandPoseEvent(this); }

        private HandPoseEvent(HandPoseEvent other) { _value = other._value; _isCaptured = true; }

        public static bool operator ==(HandPoseEvent l, HandPoseEvent r)
        {
            return l._value == r._value && l.IsCaptured == r.IsCaptured;
        }

        public static bool operator !=(HandPoseEvent l, HandPoseEvent r)
        {
            return l._value != r._value || l.IsCaptured != r.IsCaptured;
        }

        internal static HandPoseEvent Opened { get { return new HandPoseEvent(HandPoseValue.Opened); } }
        internal static HandPoseEvent Closed { get { return new HandPoseEvent(HandPoseValue.Closed); } }
        internal static HandPoseEvent Pointing1 { get { return new HandPoseEvent(HandPoseValue.PointingIndex); } }
        internal static HandPoseEvent Pointing2 { get { return new HandPoseEvent(HandPoseValue.PointingIndexAndMiddle); } }
        internal static HandPoseEvent Bloom { get { return new HandPoseEvent(HandPoseValue.Bloom); } }
        internal static HandPoseEvent ThumbsUp { get { return new HandPoseEvent(HandPoseValue.ThumbsUp); } }
        internal static HandPoseEvent Flat { get { return new HandPoseEvent(HandPoseValue.Flat); } }
        internal static HandPoseEvent Unknown { get { return new HandPoseEvent(HandPoseValue.Unknown); } }

        public static HandPoseEvent FromHandPose(HandPoseValue handPose)
        {
            switch (handPose)
            {
                case HandPoseValue.Unknown: return Unknown;
                case HandPoseValue.Opened: return Opened;
                case HandPoseValue.Closed: return Closed;
                case HandPoseValue.PointingIndex: return Pointing1;
                case HandPoseValue.PointingIndexAndMiddle: return Pointing2;
                case HandPoseValue.Flat: return Flat;
                case HandPoseValue.Bloom: return Bloom;
                case HandPoseValue.ThumbsUp: return ThumbsUp;
                default: return Unknown;
            }
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is HandPoseEvent @event &&
                   _value == @event._value &&
                   _isCaptured == @event._isCaptured;
        }

        public override int GetHashCode()
        {
            int hashCode = 1793886979;
            hashCode = hashCode * -1521134295 + _value.GetHashCode();
            hashCode = hashCode * -1521134295 + _isCaptured.GetHashCode();
            return hashCode;
        }
    }

    public class BodyPoseEventComparer : IComparer<HandPoseEvent>
    {
        internal static readonly BodyPoseEventComparer Instance = new BodyPoseEventComparer();

        public int Compare(HandPoseEvent x, HandPoseEvent y)
        {
            int delta = (int)x.Value - (int)y.Value;
            return delta;
        }
    }
}
