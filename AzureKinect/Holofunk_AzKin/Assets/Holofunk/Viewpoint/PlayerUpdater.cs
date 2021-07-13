/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using com.rfilkov.components;
using com.rfilkov.kinect;
using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Component that looks at Kinect data for the current Player, and calls UpdatePlayer() on the
    /// DistributedViewpoint.
    /// </summary>
    /// <remarks>
    /// This component is instantiated per Player that the local Viewpoint tracks.
    /// </remarks>
    public class PlayerUpdater : MonoBehaviour
    {
        public int PlayerIndex => GetComponent<SkeletonOverlayer>().playerIndex;

        private SerializedSocketAddress performerHostAddress;

        private Vector3Averager headPositionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager headForwardDirectionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager averageEyesPositionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager averageEyesForwardDirectionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager leftHandAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager rightHandAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);

        /// <summary>
        /// Set the host address for this player, once we think we know what it is.
        /// </summary>
        /// <remarks>
        /// Setting this to default(SerializedSocketAddress) is also supported, and is the correct
        /// thing to do if we lose tracking of a player, or connection to a performer.
        /// </remarks>
        public void SetPerformerHostAddress(SerializedSocketAddress performerHostAddress)
        {
            this.performerHostAddress = performerHostAddress;
        }

        public void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;

            if (kinectManager != null && kinectManager.IsInitialized())
            {
                int playerIndex = PlayerIndex;
                bool tracked = kinectManager.IsUserDetected(playerIndex);
                Player updatedPlayer;

                if (!tracked)
                {
                    updatedPlayer = new Player()
                    {
                        PlayerId = new PlayerId((byte)(playerIndex + 1)),
                        Tracked = false,
                        UserId = default(UserId),
                        PerformerHostAddress = default(SerializedSocketAddress),
                        SensorPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        HeadPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        HeadForwardDirection = new Vector3(float.NaN, float.NaN, float.NaN),
                        AverageEyesPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        AverageEyesForwardDirection = new Vector3(float.NaN, float.NaN, float.NaN),
                        LeftHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        RightHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        ViewpointPosition = new Vector3(float.NaN, float.NaN, float.NaN)
                    };
                }
                else
                {
                    ulong userId = kinectManager.GetUserIdByIndex(playerIndex);

                    headPositionAverager.Update(GetJointWorldSpacePosition(userId, KinectInterop.JointType.Head));

                    Vector3 headForwardDirection = GetJointWorldSpaceForwardDirection(userId, KinectInterop.JointType.Head);
                    headForwardDirectionAverager.Update(headForwardDirection);

                    Vector3 averageEyePosition = 
                        (GetJointWorldSpacePosition(userId, KinectInterop.JointType.EyeLeft)
                         + GetJointWorldSpacePosition(userId, KinectInterop.JointType.EyeRight))
                        / 2;
                    averageEyesPositionAverager.Update(averageEyePosition);

                    Vector3 averageEyeForwardDirection =
                        (GetJointWorldSpaceForwardDirection(userId, KinectInterop.JointType.EyeLeft)
                         + GetJointWorldSpaceForwardDirection(userId, KinectInterop.JointType.EyeRight))
                         / 2;
                    averageEyesForwardDirectionAverager.Update(averageEyeForwardDirection);

                    leftHandAverager.Update(GetJointWorldSpacePosition(userId, KinectInterop.JointType.HandLeft));
                    rightHandAverager.Update(GetJointWorldSpacePosition(userId, KinectInterop.JointType.HandRight));

                    updatedPlayer = new Player()
                    {
                        Tracked = true,
                        PlayerId = new PlayerId((byte)(playerIndex + 1)),
                        UserId = userId,
                        PerformerHostAddress = performerHostAddress,
                        SensorPosition = kinectManager.GetSensorData(0).sensorPosePosition,
                        HeadPosition = headPositionAverager.Average,
                        HeadForwardDirection = headForwardDirectionAverager.Average,
                        AverageEyesPosition = averageEyesPositionAverager.Average,
                        AverageEyesForwardDirection = averageEyesForwardDirectionAverager.Average,
                        LeftHandPosition = leftHandAverager.Average,
                        RightHandPosition = rightHandAverager.Average,
                        // hardcoded only one sensor right now
                        ViewpointPosition = kinectManager.GetSensorTransform(0).position
                    };
                }

                // We currently use the prototype Viewpoint as the owned instance for this app.
                DistributedViewpoint distributedViewpoint = Viewpoint.TheInstance;
                distributedViewpoint.UpdatePlayer(updatedPlayer);
            }
        }

        private Vector3 GetJointWorldSpacePosition(ulong userId, KinectInterop.JointType joint)
        {
            KinectManager kinectManager = KinectManager.Instance;

            Core.Contract.Requires(kinectManager != null);
            Core.Contract.Requires(kinectManager.IsInitialized());

            bool tracked = KinectManager.Instance.IsJointTracked(userId, joint);
            if (!tracked)
            {
                return Vector3.zero;
            }
            else
            {
                SkeletonOverlayer skeletonOverlayer = GetComponent<SkeletonOverlayer>();

                Vector3 posJoint = skeletonOverlayer.GetJointPosition(userId, (int)joint);
                //Debug.Log("U " + userId + " " + (KinectInterop.JointType)joint + " - pos: " + posJoint);

                /* there is never a sensor transform in current scene, so is this even a thing? let's assume not
                if (skeletonOverlayer.sensorTransform)
                {
                    posJoint = skeletonOverlayer.sensorTransform.TransformPoint(posJoint);
                }
                */

                return posJoint;
            }
        }


        private Vector3 GetJointWorldSpaceForwardDirection(ulong userId, KinectInterop.JointType joint)
        {
            KinectManager kinectManager = KinectManager.Instance;

            Core.Contract.Requires(kinectManager != null);
            Core.Contract.Requires(kinectManager.IsInitialized());

            bool tracked = KinectManager.Instance.IsJointTracked(userId, joint);
            if (!tracked)
            {
                return Vector3.zero;
            }
            else
            {
                // TODO: to flip or not to flip?
                Quaternion jointOrientation = kinectManager.GetJointOrientation(userId, joint, flip: false);
                
                // multiply orientation by a normalized forward Z vector
                Vector3 jointForwardDirection = jointOrientation * new Vector3(0, 0, 1);

                return jointForwardDirection;
            }
        }
    }
}
