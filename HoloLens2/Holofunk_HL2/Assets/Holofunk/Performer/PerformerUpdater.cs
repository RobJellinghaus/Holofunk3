﻿/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
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
    public class PerformerUpdater : MonoBehaviour
    {
        private HandPoseClassifier _classifier = new HandPoseClassifier();

        private Vector3Averager _leftHandPosAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager _rightHandPosAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager _headPosAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private TopCounter<HandPoseValue> _leftHandPoseCounter = new TopCounter<HandPoseValue>(
            MagicNumbers.FramesToAverageWhenSmoothing, (int)HandPoseValue.Max, v => (int)v, i => (HandPoseValue)i);
        private TopCounter<HandPoseValue> _rightHandPoseCounter = new TopCounter<HandPoseValue>(
            MagicNumbers.FramesToAverageWhenSmoothing, (int)HandPoseValue.Max, v => (int)v, i => (HandPoseValue)i);

        public Vector3 AverageLeftHandPos => _leftHandPosAverager.Average;
        public Vector3 AverageRightHandPos => _rightHandPosAverager.Average;
        public Vector3 AverageHeadPos => _headPosAverager.Average;

        // Update is called once per frame
        public void Update()
        {
            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            var gazeProvider = CoreServices.InputSystem.EyeGazeProvider;

            // do we currently have a Performer?
            GameObject instanceContainer = DistributedObjectFactory.FindPrototype(
                DistributedObjectFactory.DistributedType.Performer);

            (Vector3 localLeftHandPos, HandPoseValue leftHandPoseValue) = LocalHandPosition(Handedness.Left);
            (Vector3 localRightHandPos, HandPoseValue rightHandPoseValue) = LocalHandPosition( Handedness.Right);

            Vector3 localHeadPos = LocalHeadPosition(gazeProvider);

            _leftHandPosAverager.Update(localLeftHandPos);
            _rightHandPosAverager.Update(localRightHandPos);
            _headPosAverager.Update(localHeadPos);
            _leftHandPoseCounter.Update(leftHandPoseValue);
            _rightHandPoseCounter.Update(rightHandPoseValue);

            Performer performer = new Performer
            {
                LeftHandPosition = AverageLeftHandPos,
                RightHandPosition = AverageRightHandPos,
                HeadPosition = AverageHeadPos,
                LeftHandPose = new HandPose(
                    _leftHandPoseCounter.TopValue.GetValueOrDefault(HandPoseValue.Unknown)),
                RightHandPose = new HandPose(
                    _rightHandPoseCounter.TopValue.GetValueOrDefault(HandPoseValue.Unknown))
            };

            DistributedPerformer performerPrototype = instanceContainer.GetComponent<DistributedPerformer>();
            
            // Update this over the network
            performerPrototype.UpdatePerformer(performer);

            (Vector3, HandPoseValue) LocalHandPosition(Handedness h)
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

            Vector3 LocalHeadPosition(IMixedRealityEyeGazeProvider gp)
            {
                return gp.GazeOrigin;
            }
        }
    }
}
