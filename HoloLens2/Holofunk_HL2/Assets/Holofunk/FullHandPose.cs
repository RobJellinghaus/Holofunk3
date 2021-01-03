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

        /// <summary>
        /// Pretty sure hand is Spock's.
        /// </summary>
        VulcanGreeting
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
        /// The joints we track for measuring thumb pose.
        /// </summary>
        /// <remarks>
        /// The thumb's flexibility is relatively limited, and its curl is measured around only the distal joint. Otherwise
        /// it basically always looks "extended" relative to the other fingers.
        /// </remarks>
        static readonly TrackedHandJoint[] _thumbPoseJoints =
            new[] { TrackedHandJoint.ThumbProximalJoint, TrackedHandJoint.ThumbDistalJoint, TrackedHandJoint.ThumbTip };

        /// <summary>
        /// The adjacency joint lists for each finger pose.
        /// </summary>
        /// <param name="service"></param>
        static readonly TrackedHandJoint[][] _fingerPoseJoints = new[]
        {
            new[] { TrackedHandJoint.IndexMetacarpal, TrackedHandJoint.IndexKnuckle, TrackedHandJoint.IndexMiddleJoint, TrackedHandJoint.IndexDistalJoint, TrackedHandJoint.IndexTip },
            new[] { TrackedHandJoint.MiddleMetacarpal, TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.MiddleMiddleJoint, TrackedHandJoint.MiddleDistalJoint, TrackedHandJoint.MiddleTip },
            new[] { TrackedHandJoint.RingMetacarpal, TrackedHandJoint.RingKnuckle, TrackedHandJoint.RingMiddleJoint, TrackedHandJoint.RingDistalJoint, TrackedHandJoint.RingTip },
            new[] { TrackedHandJoint.PinkyMetacarpal, TrackedHandJoint.PinkyKnuckle, TrackedHandJoint.PinkyMiddleJoint, TrackedHandJoint.PinkyDistalJoint, TrackedHandJoint.PinkyTip }
        };

        /// <summary>
        /// The pairs of joints to compare for adjacency, by Finger value of the first finger in the pair.
        /// </summary>
        static readonly TrackedHandJoint[][] _fingerAdjacencyPairs = new[]
        {
            new[] { TrackedHandJoint.IndexKnuckle, TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.ThumbDistalJoint, TrackedHandJoint.IndexKnuckle },
            new[] { TrackedHandJoint.IndexKnuckle, TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.IndexTip, TrackedHandJoint.MiddleTip },
            new[] { TrackedHandJoint.MiddleKnuckle, TrackedHandJoint.RingKnuckle, TrackedHandJoint.MiddleTip, TrackedHandJoint.RingTip },
            new[] { TrackedHandJoint.RingKnuckle, TrackedHandJoint.PinkyKnuckle, TrackedHandJoint.RingDistalJoint, TrackedHandJoint.PinkyTip },
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
        /// The raw ratio values for determining finger adjacency.
        /// </summary>
        /// <remarks>
        /// Really only for debugging, shouldn't be serialized over the network.
        /// </remarks>
        readonly float[] _fingerAdjacencyRatios;

        /// <summary>
        /// The overall hand pose, if known.
        /// </summary>
        HandPose _handPose;

        /// <summary>
        /// </summary>
        /// <param name="service"></param>
        public FullHandPose()
        {
            _fingerPoses = new FingerPose[(int)Finger.Max + 1];
            _fingerColinearities = new float[(int)Finger.Max + 1];
            _fingerAdjacencies = new FingerAdjacency[(int)Finger.Max];
            _fingerAdjacencyRatios = new float[(int)Finger.Max];
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
                (FingerPose pose, float fingerColinearity) = CalculateFingerPose(service, handedness, finger);
                _fingerPoses[(int)finger] = pose;
                _fingerColinearities[(int)finger] = fingerColinearity;
            }

            // Now the finger adjacencies.
            for (Finger finger = Finger.Thumb; finger <= Finger.Ring; finger++)
            {
                (FingerAdjacency fingerAdjacency, float fingerAdjacencyRatio) = CalculateFingerAdjacency(service, handedness, finger);
                _fingerAdjacencies[(int)finger] = fingerAdjacency;
                _fingerAdjacencyRatios[(int)finger] = fingerAdjacencyRatio;
            }

            // Now classify overall hand pose.
            if (AllFingerPose(FingerPose.Extended) 
                && GetFingerPose(Finger.Thumb) == FingerPose.Extended
                && AllFingerAdjacency(FingerAdjacency.NotAdjacent)
                && GetFingerAdjacency(Finger.Thumb) == FingerAdjacency.NotAdjacent)
            {
                _handPose = HandPose.Opened;
            }
            else if (AllFingerPose(FingerPose.Curled))
            {
                _handPose = HandPose.Closed;
            }
            else if (GetFingerPose(Finger.Index) == FingerPose.Extended
                    && GetFingerPose(Finger.Middle) != FingerPose.Extended
                    && GetFingerPose(Finger.Ring) != FingerPose.Extended
                    && GetFingerPose(Finger.Pinky) != FingerPose.Extended)
            {
                _handPose = HandPose.PointingIndex;
            }
            else if (GetFingerPose(Finger.Index) == FingerPose.Extended
                && GetFingerPose(Finger.Middle) == FingerPose.Extended
                && GetFingerPose(Finger.Ring) != FingerPose.Extended
                && GetFingerPose(Finger.Pinky) != FingerPose.Extended
                && GetFingerAdjacency(Finger.Index) == FingerAdjacency.Adjacent)
            {
                _handPose = HandPose.PointingIndexAndMiddle;
            }
            else if (GetFingerPose(Finger.Index) != FingerPose.Extended
                && GetFingerPose(Finger.Middle) == FingerPose.Extended
                && GetFingerPose(Finger.Ring) != FingerPose.Extended
                && GetFingerPose(Finger.Pinky) != FingerPose.Extended)
            {
                _handPose = HandPose.PointingMiddle;
            }
            else if (GetFingerPose(Finger.Index) == FingerPose.Extended
                && GetFingerPose(Finger.Middle) == FingerPose.Extended
                && GetFingerPose(Finger.Ring) == FingerPose.Extended
                && GetFingerPose(Finger.Pinky) != FingerPose.Extended
                && GetFingerAdjacency(Finger.Index) == FingerAdjacency.Adjacent
                && GetFingerAdjacency(Finger.Middle) == FingerAdjacency.Adjacent)
            {
                _handPose = HandPose.PointingIndexMiddleAndRing;
            }
            else if (AllFingerPose(FingerPose.Extended)
                && GetFingerPose(Finger.Thumb) == FingerPose.Extended
                && GetFingerAdjacency(Finger.Thumb) != FingerAdjacency.Adjacent
                && GetFingerAdjacency(Finger.Index) == FingerAdjacency.Adjacent
                && GetFingerAdjacency(Finger.Middle) != FingerAdjacency.Adjacent
                && GetFingerAdjacency(Finger.Ring) == FingerAdjacency.Adjacent)
            {
                _handPose = HandPose.VulcanGreeting;
            }
            else if (AllFingerPose(FingerPose.Extended)
                && GetFingerPose(Finger.Thumb) == FingerPose.Extended
                && AllFingerAdjacency(FingerAdjacency.Adjacent)
                && GetFingerAdjacency(Finger.Thumb) == FingerAdjacency.Adjacent)
            {
                _handPose = HandPose.Flat;
            }
            else
            {
                _handPose = HandPose.Unknown;
            }

            bool AllFingerPose(FingerPose pose)
            {
                return GetFingerPose(Finger.Index) == pose
                    && GetFingerPose(Finger.Middle) == pose
                    && GetFingerPose(Finger.Ring) == pose
                    && GetFingerPose(Finger.Pinky) == pose;
            }

            bool AllFingerAdjacency(FingerAdjacency adjacency)
            {
                return GetFingerAdjacency(Finger.Index) == adjacency
                    && GetFingerAdjacency(Finger.Middle) == adjacency
                    && GetFingerAdjacency(Finger.Ring) == adjacency;
            }
        }

        /// <summary>
        /// Determine the pose of the given finger at the moment; this is a read-only method (mutates no state).
        /// </summary>
        /// <remarks>
        /// The algorithm is:
        /// 
        /// - For each interior joint,
        ///   - Determine the dot product of the normalized vectors entering and leaving the joint.
        ///     (In other words, effectively determine how co-linear the finger bones are at that joint.)
        ///   - Sum the dot products.
        /// - Return the sum of all dot products.
        /// 
        /// For fingers, we look at all five joints including the metacarpal, to help in determining
        /// whether the finger is pointed straight out aligned with the hand.
        /// 
        /// For the thumb, we look at only the distal joint, since the thumb has only four joints to
        /// begin with, and the thumb's metacarpal-proximal flexibility is very limited.
        /// 
        /// Note that this omits the metacarpal joints at present; this is up for debate and possible change.
        /// </remarks>
        private (FingerPose, float) CalculateFingerPose(IMixedRealityHandJointService service, Handedness handedness, Finger finger)
        {
            TrackedHandJoint[] jointsToTrack;
            if (finger == Finger.Thumb)
            {
                jointsToTrack = _thumbPoseJoints;
            }
            else
            {
                jointsToTrack = _fingerPoseJoints[(int)finger - 1];
            }

            Vector3 joint0Position = service.RequestJointTransform(jointsToTrack[0], handedness).position;
            Vector3 joint1Position = service.RequestJointTransform(jointsToTrack[1], handedness).position;
            Vector3 joint0to1 = (joint1Position - joint0Position).normalized;

            float colinearity = 0;
            for (int i = 2; i < jointsToTrack.Length; i++)
            {
                Vector3 joint2Position = service.RequestJointTransform(jointsToTrack[i], handedness).position;
                Vector3 joint1to2 = (joint2Position - joint1Position).normalized;

                float dotProduct = Vector3.Dot(joint0to1, joint1to2);
                colinearity += dotProduct;

                joint0to1 = joint1to2;
                joint1Position = joint2Position;
            }

            float extendedMinimum = finger == Finger.Thumb ? MagicNumbers.ThumbLinearityExtendedMinimum : MagicNumbers.FingerLinearityExtendedMinimum;
            float curledMaximum = finger == Finger.Thumb ? MagicNumbers.ThumbLinearityCurledMaximum : MagicNumbers.FingerLinearityCurledMaximum;
            if (colinearity >= extendedMinimum)
            {
                return (Holofunk.FingerPose.Extended, colinearity);
            }
            else if (colinearity <= curledMaximum)
            {
                return (Holofunk.FingerPose.Curled, colinearity);
            }
            else
            {
                return (Holofunk.FingerPose.Unknown, colinearity);
            }
        }

        /// <summary>
        /// Calculate the adjacency of this pair of fingers; this is a read-only method (mutates no state).
        /// </summary>
        /// <remarks>
        /// Adjacency is calculated by comparing the distance between a "base" pair of joints (typically the knuckles), and an "end"
        /// pair of joints (typically the fingertips).
        /// </remarks>
        /// <returns>
        /// The classified adjacency (or unknown), and the distance ratio value that was computed.
        /// </returns>
        private (FingerAdjacency, float) CalculateFingerAdjacency(IMixedRealityHandJointService service, Handedness handedness, Finger firstFinger)
        {
            TrackedHandJoint[] jointPairs = _fingerAdjacencyPairs[(int)firstFinger];

            float baseDistance = (JointPosition(service, handedness, jointPairs[0]) - JointPosition(service, handedness, jointPairs[1])).magnitude;
            float endDistance = (JointPosition(service, handedness, jointPairs[2]) - JointPosition(service, handedness, jointPairs[3])).magnitude;

            float ratio = endDistance / baseDistance;

            if (ratio <= MagicNumbers.FingerAdjacencyMaximum)
            {
                return (Holofunk.FingerAdjacency.Adjacent, ratio);
            }
            else if (ratio >= MagicNumbers.FingerNonAdjacencyMinimum)
            {
                return (Holofunk.FingerAdjacency.NotAdjacent, ratio);
            }
            else
            {
                return (Holofunk.FingerAdjacency.Unknown, ratio);
            }
        }

        private Vector3 JointPosition(IMixedRealityHandJointService service, Handedness handedness, TrackedHandJoint joint)
            => service.RequestJointTransform(joint, handedness).position;

        public FingerPose GetFingerPose(Finger finger) => _fingerPoses[(int)finger];

        public float GetFingerColinearity(Finger finger) => _fingerColinearities[(int)finger];

        /// <summary>
        /// Get the finger adjacency for the pair of fingers including this one (as the lower-indexed finger).
        /// </summary>
        public FingerAdjacency GetFingerAdjacency(Finger finger) => _fingerAdjacencies[(int)finger];

        /// <returns></returns>
        public float GetFingerAdjacencyRatio(Finger finger) => _fingerAdjacencyRatios[(int)finger];

        public HandPose GetHandPose() => _handPose;
    }
}