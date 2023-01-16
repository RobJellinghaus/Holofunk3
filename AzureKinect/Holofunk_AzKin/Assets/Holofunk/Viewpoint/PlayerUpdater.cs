// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using Holofunk.Loop;
using Holofunk.Sound;
using System;
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
        private Vector3Averager leftHandAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);
        private Vector3Averager rightHandAverager = new Vector3Averager(MagicNumbers.FramesToAverageWhenSmoothing);

        private DateTime trackingLostTime = default;

        public void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;

            if (kinectManager != null && kinectManager.IsInitialized())
            {
                int playerIndex = PlayerIndex;
                bool tracked = kinectManager.IsUserDetected(playerIndex);
                DistributedViewpoint distributedViewpoint = TheViewpoint.Instance;
                PlayerState currentPlayer;
                distributedViewpoint.TryGetPlayerById(new PlayerId((byte)(playerIndex + 1)), out currentPlayer);

                if (!tracked)
                {
                    if (currentPlayer.PerformerHostAddress == default)
                    {
                        currentPlayer = CreateDefaultPlayerState(playerIndex);
                    }
                    else
                    {
                        // currentPlayer was previously recognized but tracking is currently lost.
                        // If this just occurred, start a timer.
                        // If the timer is already set and has now expired, wipe the player state.
                        if (trackingLostTime == default)
                        {
                            HoloDebug.Log($"Tracking lost");
                            trackingLostTime = DateTime.Now;
                        }
                        else if (DateTime.Now - trackingLostTime > TimeSpan.FromSeconds((float)MagicNumbers.RecognitionLossDuration))
                        {
                            HoloDebug.Log($"Lost recognition after {MagicNumbers.RecognitionLossDuration} seconds");
                            // we hardly knew ye
                            currentPlayer = CreateDefaultPlayerState(playerIndex);
                        }
                    }
                }
                else
                {
                    if (trackingLostTime != default)
                    {
                        // we don't have to track anymore, we found them again
                        // TODO: actually handle multiple person recognition (sob)
                        HoloDebug.Log("Regained tracking before recognition loss; resuming tracking");
                        trackingLostTime = default;
                    }

                    long userId = kinectManager.GetUserIdByIndex(playerIndex);

                    Vector3 rawLeftHandPosition, rawRightHandPosition;
                    if (TryGetJointWorldSpacePosition(userId, KinectInterop.JointType.HandLeft, out rawLeftHandPosition))
                    {
                        leftHandAverager.Update(rawLeftHandPosition);
                    }
                    if (TryGetJointWorldSpacePosition(userId, KinectInterop.JointType.HandRight, out rawRightHandPosition))
                    {
                        rightHandAverager.Update(rawRightHandPosition);
                    }

                    // Keep the performer host address and the performer-to-viewpoint transform, if known.
                    currentPlayer = new PlayerState()
                    {
                        Tracked = true,
                        PlayerId = new PlayerId((byte)(playerIndex + 1)),
                        UserId = userId,
                        PerformerHostAddress = currentPlayer.PerformerHostAddress,
                        //SensorPosition = kinectManager.GetSensorTransform(0).position,
                        //SensorForwardDirection = kinectManager.GetSensorTransform(0).forward,
                        HeadPosition = headPositionAverager.Average,
                        HeadForwardDirection = headForwardDirectionAverager.Average,
                        LeftHandPosition = leftHandAverager.Average,
                        RightHandPosition = rightHandAverager.Average,
                        // hardcoded only one sensor right now
                        PerformerToViewpointMatrix = currentPlayer.PerformerToViewpointMatrix,
                        ViewpointToPerformerMatrix = currentPlayer.ViewpointToPerformerMatrix
                    };

                    // and now, pan the sound for this player.
                    // TODO: handle multiple audio inputs.
                    if (SoundManager.Instance != null && SoundManager.Instance.IsRunning)
                    {
                        // argh! TODO: hack sensor position
                        /*
                        Vector3 sensorPosition = currentPlayer.SensorPosition;
                        Vector3 sensorForwardDirection = currentPlayer.SensorForwardDirection;
                        Vector3 soundPosition = currentPlayer.HeadPosition;

                        float panValue = LocalLoopie.CalculatePanValue(sensorPosition, sensorForwardDirection, soundPosition);

                        NowSoundLib.NowSoundGraphAPI.SetInputPan(NowSoundLib.AudioInputId.AudioInput1, panValue);
                        */
                    }
                }

                // We currently use the prototype Viewpoint as the owned instance for this app.
                distributedViewpoint.UpdatePlayer(currentPlayer);
            }

            static PlayerState CreateDefaultPlayerState(int playerIndex)
            {
                return new PlayerState()
                {
                    PlayerId = new PlayerId((byte)(playerIndex + 1)),
                    Tracked = false,
                    UserId = default,
                    PerformerHostAddress = default,
                    //SensorPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                    //SensorForwardDirection = new Vector3(float.NaN, float.NaN, float.NaN),
                    HeadPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                    HeadForwardDirection = new Vector3(float.NaN, float.NaN, float.NaN),
                    LeftHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                    RightHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                    PerformerToViewpointMatrix = Matrix4x4.zero,
                    ViewpointToPerformerMatrix = Matrix4x4.zero
                };
            }
        }

        private bool TryGetJointWorldSpacePosition(long userId, KinectInterop.JointType joint, out Vector3 result)
        {
            KinectManager kinectManager = KinectManager.Instance;

            Core.Contract.Requires(kinectManager != null);
            Core.Contract.Requires(kinectManager.IsInitialized());

            bool tracked = KinectManager.Instance.IsJointTracked(userId, (int)joint);
            if (!tracked)
            {
                result = Vector3.zero;
                return false;
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

                result = posJoint;
                return true;
            }
        }

        private Vector3 GetJointWorldSpaceForwardDirection(long userId, KinectInterop.JointType joint)
        {
            KinectManager kinectManager = KinectManager.Instance;

            Core.Contract.Requires(kinectManager != null);
            Core.Contract.Requires(kinectManager.IsInitialized());

            bool tracked = KinectManager.Instance.IsJointTracked(userId, (int)joint);
            if (!tracked)
            {
                return new Vector3(float.NaN, float.NaN, float.NaN);
            }
            else
            {
                // TODO: to flip or not to flip?
                Quaternion jointOrientation = kinectManager.GetJointOrientation(userId, (int)joint, flip: false);
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
