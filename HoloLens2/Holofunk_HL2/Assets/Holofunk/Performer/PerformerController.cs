/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.HandComponents;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// This behavior updates the prototype Performer instance, to propagate this
    /// user's body locations (in performer space).
    /// </summary>
    public class PerformerController : MonoBehaviour
    {
        private HandPoseClassifier _classifier = new HandPoseClassifier();

        private Player ourPlayer = default(Player);

        private Vector3Averager _leftHandPosAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager _rightHandPosAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager _headPosAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager _headForwardAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private TopCounter<HandPoseValue> _leftHandPoseCounter = new TopCounter<HandPoseValue>(
            MagicNumbers.FramesToAverageWhenSmoothing, (int)HandPoseValue.Max, v => (int)v, i => (HandPoseValue)i);
        private TopCounter<HandPoseValue> _rightHandPoseCounter = new TopCounter<HandPoseValue>(
            MagicNumbers.FramesToAverageWhenSmoothing, (int)HandPoseValue.Max, v => (int)v, i => (HandPoseValue)i);

        public Vector3 AverageLeftHandPos => _leftHandPosAverager.Average;
        public Vector3 AverageRightHandPos => _rightHandPosAverager.Average;
        public Vector3 AverageHeadPos => _headPosAverager.Average;
        public Vector3 AverageHeadForwardDir => _headForwardAverager.Average;

        /// <summary>
        /// Get the state of our Player instance.
        /// </summary>
        /// <remarks>
        /// Returned by ref for efficiency, since Player is a large struct.
        /// </remarks>
        public ref Player OurPlayer => ref ourPlayer;

        // Update is called once per frame
        public void Update()
        {
            IMixedRealityHandJointService handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            IMixedRealityEyeGazeProvider gazeProvider = CoreServices.InputSystem.EyeGazeProvider;

            UpdateDistributedPerformer(handJointService, gazeProvider);

            UpdateOurPlayer();

            void UpdateOurPlayer()
            {
                // Look for our matching Player.
                // if we don't have a player with our host address, then we aren't recognized yet,
                // so do nothing.
                // TODO: handle multiple Players.
                LocalViewpoint localViewpoint = DistributedObjectFactory.FindFirstInstanceComponent<LocalViewpoint>(
                    DistributedObjectFactory.DistributedType.Viewpoint);
                ourPlayer = default(Player);
                if (localViewpoint != null)
                {
                    if (DistributedViewpoint.TheViewpoint == null)
                    {
                        DistributedViewpoint.InitializeTheViewpoint(localViewpoint.GetComponent<DistributedViewpoint>());
                    }

                    if (localViewpoint.PlayerCount > 0)
                    {
                        ourPlayer = localViewpoint.GetPlayer(0);
                    }
                }
                else
                {
                    // wipe it
                    DistributedViewpoint.InitializeTheViewpoint(null);
                }
            }
        }

        private static Vector3 LocalGazeDirection(IMixedRealityEyeGazeProvider gp)
        {
            return gp.GazeDirection;
        }

        private (Vector3, HandPoseValue) LocalHandPosition(Handedness h, IMixedRealityHandJointService handJointService, IMixedRealityEyeGazeProvider gazeProvider)
        {
            if (handJointService.IsHandTracked(h))
            {
                Vector3 position = handJointService.RequestJointTransform(TrackedHandJoint.Palm, h).position;
                _classifier.Recalculate(handJointService, gazeProvider, h);
                return (position, _classifier.GetHandPose());
            }
            else
            {
                return (new Vector3(float.NaN, float.NaN, float.NaN), HandPoseValue.Unknown);
            }
        }

        private static Vector3 LocalHeadPosition(IMixedRealityEyeGazeProvider gp)
        {
            return gp.GazeOrigin;
        }

        private void UpdateDistributedPerformer(IMixedRealityHandJointService handJointService, IMixedRealityEyeGazeProvider gazeProvider)
        {
            (Vector3 localLeftHandPos, HandPoseValue leftHandPoseValue) = LocalHandPosition(Handedness.Left, handJointService, gazeProvider);
            (Vector3 localRightHandPos, HandPoseValue rightHandPoseValue) = LocalHandPosition(Handedness.Right, handJointService, gazeProvider);

            Vector3 localHeadPos = LocalHeadPosition(gazeProvider);
            Vector3 localHeadForwardDir = LocalGazeDirection(gazeProvider);

            _leftHandPosAverager.Update(localLeftHandPos);
            _rightHandPosAverager.Update(localRightHandPos);
            _headPosAverager.Update(localHeadPos);
            _headForwardAverager.Update(localHeadForwardDir);
            _leftHandPoseCounter.Update(leftHandPoseValue);
            _rightHandPoseCounter.Update(rightHandPoseValue);

            Performer performer = new Performer
            {
                LeftHandPosition = AverageLeftHandPos,
                RightHandPosition = AverageRightHandPos,
                HeadPosition = AverageHeadPos,
                HeadForwardDirection = AverageHeadForwardDir,
                LeftHandPose = new HandPose(
                    _leftHandPoseCounter.TopValue.GetValueOrDefault(HandPoseValue.Unknown)),
                RightHandPose = new HandPose(
                    _rightHandPoseCounter.TopValue.GetValueOrDefault(HandPoseValue.Unknown))
            };

            DistributedPerformer performerPrototype =
                DistributedObjectFactory.FindPrototypeComponent<DistributedPerformer>(
                    DistributedObjectFactory.DistributedType.Performer);

            // Update this over the network
            performerPrototype.UpdatePerformer(performer);
        }
    }
}
