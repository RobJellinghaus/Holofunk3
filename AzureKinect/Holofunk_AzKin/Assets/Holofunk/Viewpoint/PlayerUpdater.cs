/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using com.rfilkov.components;
using com.rfilkov.kinect;
using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Windows.Kinect;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Component that looks at Kinect data for the current Player, and calls UpdatePlayer() on the
    /// DistributedViewpoint.
    /// </summary>
    /// <remarks>
    /// We do not want this to be done by the DistributedViewpoint object's own Update method, since
    /// this should only happen on the AzureKinect instance that owns the Viewpoint.
    /// </remarks>
    public class PlayerUpdater : MonoBehaviour
    {
        public void Update()
        {
            KinectManager kinectManager = KinectManager.Instance;

            if (kinectManager != null && kinectManager.IsInitialized())
            {
                int playerIndex = GetComponent<SkeletonOverlayer>().playerIndex;
                bool tracked = kinectManager.IsUserDetected(playerIndex);
                Player updatedPlayer;

                if (!tracked)
                {
                    updatedPlayer = new Player()
                    {
                        PlayerId = (PlayerId)playerIndex,
                        Tracked = false,
                        UserId = default(UserId),
                        PerformerId = default(PerformerId),
                        PerformerHostAddress = default(SerializedSocketAddress),
                        HeadPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        LeftHandPosition = new Vector3(float.NaN, float.NaN, float.NaN),
                        RightHandPosition = new Vector3(float.NaN, float.NaN, float.NaN)
                    };
                }
                else
                {
                    ulong userId = kinectManager.GetUserIdByIndex(playerIndex);

                    updatedPlayer = new Player()
                    {
                        Tracked = true,
                        PlayerId = (PlayerId)playerIndex,
                        UserId = (UserId)userId,
                        PerformerId = default(PerformerId),
                        PerformerHostAddress = default(SerializedSocketAddress),
                        HeadPosition = GetJointWorldSpacePosition(userId, KinectInterop.JointType.Head),
                        LeftHandPosition = GetJointWorldSpacePosition(userId, KinectInterop.JointType.HandLeft),
                        RightHandPosition = GetJointWorldSpacePosition(userId, KinectInterop.JointType.HandRight)
                    };
                }

                // We currently use the prototype Viewpoint as the owned instance for this app.
                // TODO: is this simplest/best?
                GameObject viewpointPrototype = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Viewpoint);
                viewpointPrototype.GetComponent<DistributedViewpoint>().UpdatePlayer(updatedPlayer);
            }
        }

        private Vector3 GetJointWorldSpacePosition(ulong userId, KinectInterop.JointType joint)
        {
            KinectManager kinectManager = KinectManager.Instance;

            Contract.Requires(kinectManager != null);
            Contract.Requires(kinectManager.IsInitialized());

            bool tracked = KinectManager.Instance.IsJointTracked(userId, joint);
            if (!tracked)
            {
                return new Vector3 { x = float.NaN, y = float.NaN, z = float.NaN };
            }
            else
            {
                SkeletonOverlayer skeletonOverlayer = GetComponent<SkeletonOverlayer>();

                Vector3 posJoint = skeletonOverlayer.GetJointPosition(userId, (int)joint);
                //Debug.Log("U " + userId + " " + (KinectInterop.JointType)joint + " - pos: " + posJoint);

                if (skeletonOverlayer.sensorTransform)
                {
                    posJoint = skeletonOverlayer.sensorTransform.TransformPoint(posJoint);
                }

                return posJoint;
            }
        }
    }
}
