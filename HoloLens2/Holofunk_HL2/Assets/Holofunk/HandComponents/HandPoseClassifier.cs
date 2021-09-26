/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Hand;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Holofunk.HandComponents
{
    /// <summary>
    /// Hand pose, along with all finger poses and adjacencies.
    /// </summary>
    public class HandPoseClassifier
    {
        #region Fields (all computed by Recalculate)

        /// <summary>
        /// The joints we track for measuring thumb pose.
        /// </summary>
        /// <remarks>
        /// The thumb's flexibility is relatively limited, and its curl is measured around only the distal joint. Otherwise
        /// it basically always looks "extended" relative to the other fingers.
        /// </remarks>
        static readonly TrackedHandJoint[] _thumbPoseJoints =
            new[] { TrackedHandJoint.ThumbMetacarpalJoint, TrackedHandJoint.ThumbProximalJoint, TrackedHandJoint.ThumbDistalJoint, TrackedHandJoint.ThumbTip };

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
        /// The five finger poses.
        /// </summary>
        readonly FingerPose[] _fingerPoses;

        /// <summary>
        /// The raw colinearity values for the interior knuckles of each finger.
        /// </summary>
        /// <remarks>
        /// Really only for debugging, shouldn't be serialized over the network.
        /// </remarks>
        readonly float[] _jointColinearities;

        /// <summary>
        /// The four finger co-extensions (thumb-index; index-middle; middle-ring; ring-pinky).
        /// </summary>
        readonly FingerPairExtension[] _fingerPairExtensions;

        /// <summary>
        /// The raw colinearities between adjacent pairs of fingers.
        /// </summary>
        /// <remarks>
        /// Really only for debugging, shouldn't be serialized over the network.
        /// </remarks>
        readonly float[] _fingerPairColinearities;

        /// <summary>
        /// The colinearities between the eye->knuckle vector, and the knuckle->fingertip vector, per finger
        /// (thumb included).
        /// </summary>
        /// <remarks>
        /// A closed hand, with all fingers on the other side of the hand from the eye, results in the HL2
        /// guessing at the poses of the non-visible fingers. This measurement lets us determine whether
        /// the fingers are aligned with the eye, which will tell us whether they are effectively not
        /// actually visible. Theoretically.
        /// </remarks>
        readonly float[] _fingerEyeColinearities;

        /// <summary>
        /// The sum of the distances between neighboring fingertips.
        /// </summary>
        float _sumPairwiseFingertipDistances;

        /// <summary>
        /// The sum of the distances between neighboring knuckles.
        /// </summary>
        float _sumPairwiseKnuckleDistances;

        /// <summary>
        /// The Y distance between the thumb tip and the thumb base.
        /// </summary>
        float _thumbTipAltitude;

        /// <summary>
        /// The thumb's vector (proximal joint -> tip) dotted with Vector3.Up.
        /// </summary>
        /// <remarks>
        /// This is more reliable than thumb tip altitude.
        /// </remarks>
        float _thumbVectorDotUp;

        /// <summary>
        /// The minimum values of all joint coordinates (in X, Y, Z axes).
        /// </summary>
        Vector3 _minJointCoordinates;

        /// <summary>
        /// The maximum values of all joint coordinates (in X, Y, Z axes).
        /// </summary>
        Vector3 _maxJointCoordinates;

        /// <summary>
        /// _maxJointCoordinates - _minJointCoordinates
        /// </summary>
        /// <remarks>
        /// Used to determine hand flatness. If Y value of this is smallest (by a magic number factor), the
        /// hand is presumed flat.
        /// </remarks>
        Vector3 _netJointDisplacement;

        /// <summary>
        /// The overall hand pose.
        /// </summary>
        HandPoseValue _handPose;

        #endregion

        /// <summary>
        /// Construct a new hand pose instance.
        /// </summary>
        /// <param name="service"></param>
        public HandPoseClassifier()
        {
            _fingerPoses = new FingerPose[(int)Finger.Max + 1];
            _jointColinearities = new float[(int)Finger.Max + 1];
            _fingerPairExtensions = new FingerPairExtension[(int)Finger.Max];
            _fingerPairColinearities = new float[(int)Finger.Max];
            _fingerEyeColinearities = new float[(int)Finger.Max + 1];
        }

        /// <summary>Recalculate this hand's pose based on the current joint positions of the given hand.</summary>
        public void Recalculate(
            IMixedRealityHandJointService handJointService,
            IMixedRealityGazeProvider gazeProvider,
            Handedness handedness)
        {
            // First determine the finger poses.
            _sumPairwiseFingertipDistances = 0;
            _sumPairwiseKnuckleDistances = 0;
            _minJointCoordinates = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            _maxJointCoordinates = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (Finger finger = Finger.Thumb; finger <= Finger.Max; finger++)
            {
                (FingerPose pose, float fingerColinearity) = CalculateFingerPose(handJointService, handedness, finger);
                _fingerPoses[(int)finger] = pose;
                _jointColinearities[(int)finger] = fingerColinearity;

                // Now the eye->knuckle colinearities.
                float fingerEyeColinearity = CalculateFingerEyeColinearity(handJointService, gazeProvider, handedness, finger);
                _fingerEyeColinearities[(int)finger] = fingerEyeColinearity;

                if (finger < Finger.Pinky)
                {
                    // Now the finger extensions.
                    (FingerPairExtension fingerExtension, float fingerPairColinearity) = CalculateFingerExtension(handJointService, handedness, finger);
                    _fingerPairExtensions[(int)finger] = fingerExtension;
                    _fingerPairColinearities[(int)finger] = fingerPairColinearity;

                    // Now add in the distances between fingertips and knuckles, for bloom gesture detection.
                    TrackedHandJoint[] finger0Joints = finger == Finger.Thumb ? _thumbPoseJoints : _fingerPoseJoints[(int)finger - 1];
                    TrackedHandJoint[] finger1Joints = _fingerPoseJoints[(int)finger];
                    // add in the fingertip-to-fingertip and knuckle-to-knuckle distances
                    Vector3 finger0knuckle = JointPosition(handJointService, handedness, finger0Joints[1]);
                    Vector3 finger1knuckle = JointPosition(handJointService, handedness, finger1Joints[1]);

                    Vector3 finger0tip = JointPosition(handJointService, handedness, finger0Joints[finger0Joints.Length - 1]);
                    Vector3 finger1tip = JointPosition(handJointService, handedness, finger1Joints[finger1Joints.Length - 1]);

                    _sumPairwiseKnuckleDistances += (finger0knuckle - finger1knuckle).magnitude;
                    _sumPairwiseFingertipDistances += (finger0tip - finger1tip).magnitude;
                }

                Vector3 thumbTipPosition = JointPosition(handJointService, handedness, TrackedHandJoint.ThumbTip);
                Vector3 thumbProximalPosition = JointPosition(handJointService, handedness, TrackedHandJoint.ThumbProximalJoint);
                _thumbTipAltitude = thumbTipPosition.y - thumbProximalPosition.y;
                _thumbVectorDotUp = Vector3.Dot((thumbTipPosition - thumbProximalPosition).normalized, Vector3.up);

                // Now the min/avg/max joint positions.
                TrackedHandJoint[] jointsToTrack = finger == Finger.Thumb ? _thumbPoseJoints : _fingerPoseJoints[(int)finger - 1];
                UpdateMinMaxAvg(JointPosition(handJointService, handedness, jointsToTrack[0]));
                UpdateMinMaxAvg(JointPosition(handJointService, handedness, jointsToTrack[1]));

                void UpdateMinMaxAvg(Vector3 jointPosition)
                {
                    _minJointCoordinates = new Vector3(
                        Mathf.Min(_minJointCoordinates.x, jointPosition.x),
                        Mathf.Min(_minJointCoordinates.y, jointPosition.y),
                        Mathf.Min(_minJointCoordinates.z, jointPosition.z));
                    _maxJointCoordinates = new Vector3(
                        Mathf.Max(_maxJointCoordinates.x, jointPosition.x),
                        Mathf.Max(_maxJointCoordinates.y, jointPosition.y),
                        Mathf.Max(_maxJointCoordinates.z, jointPosition.z));
                }
            }

            _netJointDisplacement = _maxJointCoordinates - _minJointCoordinates;

            ClassifyHandPose();
        }

        /// <summary>
        /// Once all individual finger poses and measurements are known, classify the overall hand pose.
        /// </summary>
        private void ClassifyHandPose()
        {
            // Now classify overall hand pose.
            if (AllFingerPose(FingerPose.Extended)
                && GetFingerPose(Finger.Thumb) == FingerPose.Extended
                && GetFingerPairExtension(Finger.Thumb) == FingerPairExtension.NotExtendedTogether
                && (_sumPairwiseFingertipDistances / _sumPairwiseKnuckleDistances)
                     > HandPoseMagicNumbers.FingertipSumDistanceToKnuckleSumDistanceRatioMaximum)
            {
                _handPose = HandPoseValue.Opened;
            }
            else if (GetFingerPose(Finger.Index) == FingerPose.Extended
                    && GetFingerPose(Finger.Middle) != FingerPose.Extended
                    && GetFingerPose(Finger.Ring) != FingerPose.Extended
                    && GetFingerPose(Finger.Pinky) != FingerPose.Extended)
            {
                _handPose = HandPoseValue.PointingIndex;
            }
            /* UNRELIABLE
            else if (GetFingerPose(Finger.Index) == FingerPose.Extended
                && GetFingerPose(Finger.Middle) == FingerPose.Extended
                && GetFingerPose(Finger.Ring) != FingerPose.Extended
                && GetFingerPose(Finger.Pinky) != FingerPose.Extended)
            {
                _handPose = HandPoseValue.PointingIndexAndMiddle;
            }
            else if (GetFingerPose(Finger.Index) != FingerPose.Extended
                && GetFingerPose(Finger.Middle) == FingerPose.Extended
                && GetFingerPose(Finger.Ring) != FingerPose.Extended
                && GetFingerPose(Finger.Pinky) != FingerPose.Extended)
            {
                _handPose = HandPoseValue.PointingMiddle;
            }
            */
            // If all fingertips are close together and all are above their respective knuckles,
            // then consider it the bloom gesture.
            else if ((_sumPairwiseFingertipDistances / _sumPairwiseKnuckleDistances)
                     <= HandPoseMagicNumbers.FingertipSumDistanceToKnuckleSumDistanceRatioMaximum
                     && GetFingerJointColinearity(Finger.Index) > HandPoseMagicNumbers.FingerLinearityExtendedMinimum
                     && GetFingerJointColinearity(Finger.Middle) > HandPoseMagicNumbers.FingerLinearityExtendedMinimum)
            {
                _handPose = HandPoseValue.Bloom;
            }
            // If all fingers are non-extended, then the thumb determines whether we are closed or not.
            // We detect thumbs up only.
            // If all fingers are curled, or the thumb is curled and all the other fingers are aligned with the eye,
            // then consider the hand to be closed.
            else if (NoFingerPose(FingerPose.Extended))
            {
                if (GetFingerPose(Finger.Thumb) == FingerPose.Extended
                    && _thumbVectorDotUp > HandPoseMagicNumbers.ThumbVectorDotUpMinimum)
                {
                    _handPose = HandPoseValue.ThumbsUp;
                }
                else
                {
                    _handPose = HandPoseValue.Closed;
                }
            }
            else if (AllFingerPose(FingerPose.Extended)
                && GetFingerPose(Finger.Thumb) == FingerPose.Extended
                && NoFingerExtension(FingerPairExtension.NotExtendedTogether)
                && GetFingerPairExtension(Finger.Thumb) != FingerPairExtension.NotExtendedTogether
                && _netJointDisplacement.x > HandPoseMagicNumbers.HandFlatnessFactor * _netJointDisplacement.y
                && _netJointDisplacement.z > HandPoseMagicNumbers.HandFlatnessFactor * _netJointDisplacement.y)
            {
                _handPose = HandPoseValue.Flat;
            }
            else
            {
                _handPose = HandPoseValue.Unknown;
            }

            bool AllFingerPose(FingerPose pose)
            {
                return GetFingerPose(Finger.Index) == pose
                    && GetFingerPose(Finger.Middle) == pose
                    && GetFingerPose(Finger.Ring) == pose
                    && GetFingerPose(Finger.Pinky) == pose;
            }

            bool NoFingerPose(FingerPose pose)
            {
                return GetFingerPose(Finger.Index) != pose
                    && GetFingerPose(Finger.Middle) != pose
                    && GetFingerPose(Finger.Ring) != pose
                    && GetFingerPose(Finger.Pinky) != pose;
            }

            bool NoFingerExtension(FingerPairExtension extension)
            {
                return GetFingerPairExtension(Finger.Index) != extension
                    && GetFingerPairExtension(Finger.Middle) != extension
                    && GetFingerPairExtension(Finger.Ring) != extension;
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
            TrackedHandJoint[] jointsToTrack = finger == Finger.Thumb ? _thumbPoseJoints : _fingerPoseJoints[(int)finger - 1];

            Vector3 joint0Position = JointPosition(service, handedness, jointsToTrack[0]);
            Vector3 joint1Position = JointPosition(service, handedness, jointsToTrack[1]);
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

            float extendedMinimum = finger == Finger.Thumb ? HandPoseMagicNumbers.ThumbLinearityExtendedMinimum : HandPoseMagicNumbers.FingerLinearityExtendedMinimum;
            float curledMaximum = finger == Finger.Thumb ? HandPoseMagicNumbers.ThumbLinearityCurledMaximum : HandPoseMagicNumbers.FingerLinearityCurledMaximum;
            if (colinearity >= extendedMinimum)
            {
                return (FingerPose.Extended, colinearity);
            }
            else if (colinearity <= curledMaximum)
            {
                return (FingerPose.Curled, colinearity);
            }
            else
            {
                return (FingerPose.Unknown, colinearity);
            }
        }

        /// <summary>
        /// Calculate the colinearity of this pair of fingers; this is a read-only method (mutates no state).
        /// </summary>
        /// <remarks>
        /// Colinearity is calculated by getting the dot product of the normalized vectors between the fingers' knuckles and fingertips.
        /// </remarks>
        /// <returns>
        /// The classified colinearity (or unknown), and the colinearity value that was computed.
        /// </returns>
        private (FingerPairExtension, float) CalculateFingerExtension(IMixedRealityHandJointService service, Handedness handedness, Finger firstFinger)
        {
            HoloDebug.Assert(firstFinger < Finger.Pinky);

            TrackedHandJoint[] finger0Joints = firstFinger == Finger.Thumb ? _thumbPoseJoints : _fingerPoseJoints[(int)firstFinger - 1];
            TrackedHandJoint[] finger1Joints = _fingerPoseJoints[(int)firstFinger];

            Vector3 knuckleToFingertip0 = JointPosition(service, handedness, finger0Joints[finger0Joints.Length - 1])
                - JointPosition(service, handedness, finger0Joints[1]);
            Vector3 knuckleToFingertip1 = JointPosition(service, handedness, finger1Joints[finger1Joints.Length - 1])
                - JointPosition(service, handedness, finger1Joints[1]);

            float colinearity = Vector3.Dot(knuckleToFingertip0.normalized, knuckleToFingertip1.normalized);

            if (colinearity >= HandPoseMagicNumbers.FingersExtendedColinearityMinimum)
            {
                return (FingerPairExtension.ExtendedTogether, colinearity);
            }
            else if (colinearity <= HandPoseMagicNumbers.FingersNotExtendedColinearityMaximum)
            {
                return (FingerPairExtension.NotExtendedTogether, colinearity);
            }
            else
            {
                return (FingerPairExtension.Unknown, colinearity);
            }
        }

        /// <summary>
        /// Calculate how aligned this finger is with the vector from the eye to the knuckle.
        /// </summary>
        /// <remarks>
        /// This is calculated by determining the colinearity of the eye->knuckle vector with the knuckle->fingertip vector.
        /// </remarks>
        /// <returns>
        /// The colinearity value that was computed.
        /// </returns>
        private float CalculateFingerEyeColinearity(
            IMixedRealityHandJointService handJointService, 
            IMixedRealityGazeProvider gazeProvider, 
            Handedness handedness, 
            Finger firstFinger)
        {
            TrackedHandJoint[] fingerJoints = firstFinger == Finger.Thumb ? _thumbPoseJoints : _fingerPoseJoints[(int)firstFinger - 1];

            Vector3 knuckleToFingertip = JointPosition(handJointService, handedness, fingerJoints[fingerJoints.Length - 1])
                - JointPosition(handJointService, handedness, fingerJoints[1]);

            Vector3 eyeToKnuckle = JointPosition(handJointService, handedness, fingerJoints[1]) - gazeProvider.GazeOrigin;

            float colinearity = Vector3.Dot(knuckleToFingertip.normalized, eyeToKnuckle.normalized);

            return colinearity;
        }

        private Vector3 JointPosition(IMixedRealityHandJointService service, Handedness handedness, TrackedHandJoint joint)
            => service.RequestJointTransform(joint, handedness).position;

        public FingerPose GetFingerPose(Finger finger) => _fingerPoses[(int)finger];

        public float GetFingerJointColinearity(Finger finger) => _jointColinearities[(int)finger];

        /// <summary>
        /// Get the finger adjacency for the pair of fingers including this one (as the lower-indexed finger).
        /// </summary>
        public FingerPairExtension GetFingerPairExtension(Finger finger)
        {
            HoloDebug.Assert(finger < Finger.Pinky);
            return _fingerPairExtensions[(int)finger];
        }

        /// <returns></returns>
        public float GetFingerPairColinearity(Finger finger)
        {
            HoloDebug.Assert(finger < Finger.Pinky);
            return _fingerPairColinearities[(int)finger];
        }

        public float GetFingerEyeColinearity(Finger finger) => _fingerEyeColinearities[(int)finger];

        public float GetSumPairwiseFingertipDistances() => _sumPairwiseFingertipDistances;

        public float GetSumPairwiseKnuckleDistances() => _sumPairwiseKnuckleDistances;

        public float GetThumbTipAltitude() => _thumbTipAltitude;

        public float GetThumbVectorDotUp() => _thumbVectorDotUp;

        public HandPoseValue GetHandPose() => _handPose;
    }
}