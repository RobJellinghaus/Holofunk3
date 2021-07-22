// Copyright by Rob Jellinghaus. All rights reserved.

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

        private Vector3Averager headPositionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager headForwardDirectionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager averageEyesPositionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager averageEyesForwardDirectionAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager leftHandAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager rightHandAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);

        public void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;

            if (kinectManager != null && kinectManager.IsInitialized())
            {
                int playerIndex = PlayerIndex;
                bool tracked = kinectManager.IsUserDetected(playerIndex);
                DistributedViewpoint distributedViewpoint = Viewpoint.TheInstance;
                Player currentPlayer;
                distributedViewpoint.TryGetPlayer(new PlayerId((byte)(playerIndex + 1)), out currentPlayer);

                if (!tracked)
                {
                    currentPlayer = new Player()
                    {
                        PlayerId = new PlayerId((byte)(playerIndex + 1)),
                        Tracked = false,
                        UserId = default(UserId),
                        PerformerHostAddress = default(SerializedSocketAddress),
                        SensorPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        SensorForwardDirection = new Vector3(float.NaN, float.NaN, float.NaN),
                        HeadPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        HeadForwardDirection = new Vector3(float.NaN, float.NaN, float.NaN),
                        LeftHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        RightHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        PerformerToViewpointTransform = Matrix4x4.zero,
                        ViewpointToPerformerTransform = Matrix4x4.zero
                    };
                }
                else
                {
                    ulong userId = kinectManager.GetUserIdByIndex(playerIndex);

                    headPositionAverager.Update(GetJointWorldSpacePosition(userId, KinectInterop.JointType.Nose));

                    Vector3 headForwardDirection = GetJointWorldSpaceForwardDirection(userId, KinectInterop.JointType.Nose);
                    headForwardDirectionAverager.Update(headForwardDirection);

                    leftHandAverager.Update(GetJointWorldSpacePosition(userId, KinectInterop.JointType.HandLeft));
                    rightHandAverager.Update(GetJointWorldSpacePosition(userId, KinectInterop.JointType.HandRight));

                    // Keep the performer host address and the performer-to-viewpoint transform, if known.
                    currentPlayer = new Player()
                    {
                        Tracked = true,
                        PlayerId = new PlayerId((byte)(playerIndex + 1)),
                        UserId = userId,
                        PerformerHostAddress = currentPlayer.PerformerHostAddress,
                        SensorPosition = kinectManager.GetSensorTransform(0).position,
                        SensorForwardDirection = kinectManager.GetSensorTransform(0).forward,
                        HeadPosition = headPositionAverager.Average,
                        HeadForwardDirection = headForwardDirectionAverager.Average,
                        LeftHandPosition = leftHandAverager.Average,
                        RightHandPosition = rightHandAverager.Average,
                        // hardcoded only one sensor right now
                        PerformerToViewpointTransform = currentPlayer.PerformerToViewpointTransform,
                        ViewpointToPerformerTransform = currentPlayer.ViewpointToPerformerTransform
                    };
                }

                // We currently use the prototype Viewpoint as the owned instance for this app.
                distributedViewpoint.UpdatePlayer(currentPlayer);
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
                return new Vector3(float.NaN, float.NaN, float.NaN);
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
                return new Vector3(float.NaN, float.NaN, float.NaN);
            }
            else
            {
                // TODO: to flip or not to flip?
                Quaternion jointOrientation = kinectManager.GetJointOrientation(userId, joint, flip: false);
                // multiply orientation by a normalized forward Z vector
                Vector3 jointForwardDirection = jointOrientation * new Vector3(0, 0, 1);

                // NOW, TOTAL HACK: reverse the Z direction.
                // We observe that the camera's forward direction is (0, 0, 1).
                // But looking straight at the camera also yields a forward direction of (0, 0, 1).
                // This seems impossible. The forward direction should be the opposite of the camera
                // look direction. So, invert the Z coordinate before returning.
                jointForwardDirection.z = -jointForwardDirection.z;

                return jointForwardDirection;
            }
        }
    }
}
