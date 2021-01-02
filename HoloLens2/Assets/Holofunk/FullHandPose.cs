/// Copyright by Rob Jellinghaus.  All rights reserved.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Holofunk
{
    /// <summary>
    /// Which pose is each finger in?
    /// </summary>
    /// <remarks>
    /// Curled is as in making a fist; Extended is straight out; Unknown is anything in between.
    /// </remarks>
    public enum FingerPose
    {
        /// <summary>
        /// We don't know what pose this finger is in.
        /// </summary>,
        Unknown,
        
        /// <summary>
        /// We are pretty sure this finger is curled up (as when making a fist).
        /// </summary>
        Curled,

        /// <summary>
        /// We are pretty sure this finger is extended more or less straight out.
        /// </summary>
        Extended
    }

    /// <summary>
    /// For each pair of adjacent fingers, how adjacent are they?
    /// </summary>
    /// <remarks>
    /// This is calculated by determining the distances between neighboring knuckles, relative to the
    /// distance between the two base knuckles. Note that currently this only applies to the four
    /// fingers, not the thumb (which doesn't have a good adjacent-base-knuckle baseline to normalize
    /// against).
    /// </remarks>
    public enum FingerAdjacency
    {
        /// <summary>
        /// We don't know how close this pair of fingers are.
        /// </summary>
        Unknown,

        /// <summary>
        /// We are pretty confident these two fingertips are not adjacent.
        /// </summary>
        NotAdjacent,

        /// <summary>
        /// We are pretty confident these two fingers are adjacent.
        /// </summary>
        Adjacent
    }

    /// <summary>
    /// What overall shape do we think the hand is in?
    /// </summary>
    public enum HandPose
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
        PointingIndexAndMiddle,

        /// <summary>
        /// Pretty sure hand is pointing with index, middle, and ring fingers adjacent.
        /// </summary>
        PointingIndexMiddleAndRing,

        /// <summary>
        /// Pretty sure hand is fully flat with all fingers extended and adjacent.
        /// </summary>
        Flat,
    }

    /// <summary>
    /// Which finger is which?
    /// </summary>
    public enum Finger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky,
        Max = Pinky
    }

    /// <summary>
    /// Hand pose, along with all finger poses and adjacencies.
    /// </summary>
    /// <remarks>
    /// Allocates two very small backing arrays.
    /// </remarks>
    public class FullHandPose
    {
        /// <summary>
        /// The adjacency joint lists for each finger pose, indexed by Finger value.
        /// </summary>
        /// <param name="service"></param>
        static readonly TrackedHandJoint[][] _fingerPoseJoints = new[]
        {
            new[] { TrackedHandJoint.ThumbMetacarpalJoint, TrackedHandJoint.ThumbProximalJoint, TrackedHandJoint.ThumbDistalJoint, TrackedHandJoint.ThumbTip },
            new[] { TrackedHandJoint.IndexKnuckle, TrackedHandJoint.IndexMiddleJoint, TrackedHandJoint.IndexDistalJoint, TrackedHandJoint.IndexTip },
            new[] { TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.MiddleMiddleJoint, TrackedHandJoint.MiddleDistalJoint, TrackedHandJoint.MiddleTip },
            new[] { TrackedHandJoint.RingKnuckle, TrackedHandJoint.RingMiddleJoint, TrackedHandJoint.RingDistalJoint, TrackedHandJoint.RingTip },
            new[] { TrackedHandJoint.PinkyKnuckle, TrackedHandJoint.PinkyMiddleJoint, TrackedHandJoint.PinkyDistalJoint, TrackedHandJoint.PinkyTip }
        };

        /// <summary>
        /// The pairs of joints to compare for adjacency, by Finger value of the first finger in the pair.
        /// </summary>
        static readonly TrackedHandJoint[][] _fingerAdjacencyPairs = new[]
        {
            new[] { TrackedHandJoint.IndexKnuckle, TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.ThumbDistalJoint, TrackedHandJoint.IndexKnuckle },
            new[] { TrackedHandJoint.IndexKnuckle, TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.IndexMiddleJoint, TrackedHandJoint.MiddleMiddleJoint },
            new[] { TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.RingKnuckle, TrackedHandJoint.MiddleMiddleJoint, TrackedHandJoint.RingMiddleJoint },
            new[] { TrackedHandJoint.RingKnuckle, TrackedHandJoint.PinkyKnuckle, TrackedHandJoint.RingMiddleJoint, TrackedHandJoint.PinkyMiddleJoint },
        };

        /// <summary>
        /// The five finger poses.
        /// </summary>
        readonly FingerPose[] _fingerPoses;

        /// <summary>
        /// The raw colinearity values for the interior knuckles of each finger.
        /// </summary>
        /// <remarks>
        /// Really only for debugging, shouldn't be serialized over the network.
        /// </remarks>
        readonly float[] _fingerColinearities;

        /// <summary>
        /// The three finger adjacencies (index-middle; middle-ring; ring-pinky).
        /// </summary>
        readonly FingerAdjacency[] _fingerAdjacencies;

        /// <summary>
        /// The overall hand pose, if known.
        /// </summary>
        readonly HandPose _handPose;

        /// <summary>
        /// </summary>
        /// <param name="service"></param>
        public FullHandPose()
        {
            _fingerPoses = new FingerPose[(int)Finger.Max + 1];
            _fingerColinearities = new float[(int)Finger.Max + 1];
            _fingerAdjacencies = new FingerAdjacency[(int)Finger.Max - 1];
        }

        /// <summary>
        /// Recalculate this pose based on the current joint positions of the given hand.
        /// </summary>
        /// <param name="service"></param>
        public void Recalculate(IMixedRealityHandJointService service, Handedness handedness)
        {
            // First determine the finger poses.
            for (Finger finger = Finger.Thumb; finger <= Finger.Max; finger++)
            {
                FingerPose pose = CalculateFingerPose(service, handedness, finger);
                _fingerPoses[(int)finger] = pose;
            }

            /*
            // Now the finger adjacencies.
            for (Finger finger = Finger.Thumb; finger <= Finger.Ring; finger++)
            {
                FingerAdjacency fingerAdjacency = CalculateFingerAdjacency(service, handedness, finger);
                _fingerAdjacencies[(int)finger] = fingerAdjacency;
            }
            */

            // Now the hand pose... well, soon.
        }

        /// <summary>
        /// Determine the pose of the given finger at the moment.
        /// </summary>
        /// <remarks>
        /// The algorithm is:
        /// 
        /// - For each interior (middle or distal) knuckle,
        ///   - Determine the dot product of the normalized vectors entering and leaving the knuckle.
        ///     (In other words, effectively determine how co-linear the finger bones are at that knuckle.)
        ///   - Sum the dot products.
        /// - Return the sum of all dot products.
        /// 
        /// Perfectly extended fingers will have a value of 2. Fingers bent at 90 degree angles at each
        /// interior knuckle will have a value of 0. Fingers curled even more tightly will have a negative
        /// value.
        /// 
        /// Note that this omits the metacarpal joints at present; this is up for debate and possible change.
        /// </remarks>
        private FingerPose CalculateFingerPose(IMixedRealityHandJointService service, Handedness handedness, Finger finger)
        {
            TrackedHandJoint[] jointsForFinger = _fingerPoseJoints[(int)finger];
            TrackedHandJoint joint0 = jointsForFinger[0];
            TrackedHandJoint joint1 = jointsForFinger[1];
            TrackedHandJoint joint2 = jointsForFinger[2];
            TrackedHandJoint joint3 = jointsForFinger[3];

            Vector3 joint0Position = service.RequestJointTransform(joint0, handedness).position;
            Vector3 joint1Position = service.RequestJointTransform(joint1, handedness).position;
            Vector3 joint2Position = service.RequestJointTransform(joint2, handedness).position;
            Vector3 joint3Position = service.RequestJointTransform(joint3, handedness).position;

            // Colinearity at joint #1
            float firstDot = Vector3.Dot((joint1Position - joint0Position).normalized, (joint2Position - joint1Position).normalized);
            // Colinearity at joint #2
            float secondDot = Vector3.Dot((joint2Position - joint1Position).normalized, (joint3Position - joint2Position).normalized);

            float colinearity = firstDot + secondDot;
            _fingerColinearities[(int)finger] = colinearity;

            if (colinearity >= MagicNumbers.FingerLinearityExtendedMinimum)
            {
                return Holofunk.FingerPose.Extended;
            }
            else if (colinearity <= MagicNumbers.FingerLinearityCurledMaximum)
            {
                return Holofunk.FingerPose.Curled;
            }
            else
            {
                return Holofunk.FingerPose.Unknown;
            }
        }

        public FingerPose FingerPose(Finger finger) => _fingerPoses[(int)finger];

        public float FingerColinearity(Finger finger) => _fingerColinearities[(int)finger];
    }
}