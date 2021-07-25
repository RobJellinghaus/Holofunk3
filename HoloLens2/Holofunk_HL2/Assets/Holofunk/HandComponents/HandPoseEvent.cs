﻿/// Copyright by Rob Jellinghaus.  All rights reserved.

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
        readonly HandPoseValue _type;
        readonly bool _isCaptured;

        /// <summary>The type of event.</summary>
        internal HandPoseValue Type { get { return _type; } }

        /// <summary>
        /// Has this event already been captured?
        /// </summary>
        /// <remarks>
        /// We may still want to respond to already-captured events in some way (e.g. by changing a hand sprite),
        /// but we want to be able to detect that the capture already took place, and modify the transition
        /// appropriately.
        /// </remarks>
        internal bool IsCaptured { get { return _isCaptured; } }

        internal HandPoseEvent(HandPoseValue type) { _type = type; _isCaptured = false; }

        /// <summary>
        /// Construct a captured BodyPoseEvent from an existing one.
        /// </summary>
        internal HandPoseEvent AsCaptured() { return new HandPoseEvent(this); }

        private HandPoseEvent(HandPoseEvent other) { _type = other._type; _isCaptured = true; }

        internal static HandPoseEvent Opened { get { return new HandPoseEvent(HandPoseValue.Opened); } }
        internal static HandPoseEvent Closed { get { return new HandPoseEvent(HandPoseValue.Closed); } }
        internal static HandPoseEvent Pointing1 { get { return new HandPoseEvent(HandPoseValue.PointingIndex); } }
        internal static HandPoseEvent Pointing2 { get { return new HandPoseEvent(HandPoseValue.PointingIndexAndMiddle); } }
        internal static HandPoseEvent Bloom { get { return new HandPoseEvent(HandPoseValue.Bloom); } }
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
                default: return Unknown;
            }
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }

    public class BodyPoseEventComparer : IComparer<HandPoseEvent>
    {
        internal static readonly BodyPoseEventComparer Instance = new BodyPoseEventComparer();

        public int Compare(HandPoseEvent x, HandPoseEvent y)
        {
            int delta = (int)x.Type - (int)y.Type;
            return delta;
        }
    }
}
