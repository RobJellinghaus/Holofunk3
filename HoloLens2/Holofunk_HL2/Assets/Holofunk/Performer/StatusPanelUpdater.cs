/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System;
using UnityEngine;

namespace Holofunk.HandPose
{
    /// <summary>
    /// This behavior expects to inhabit a "FloatingTextPanel" GameObject.
    /// </summary>
    public class StatusPanelUpdater : MonoBehaviour
    {
        private HandPoseClassifier _classifier = new HandPoseClassifier();

        // Update is called once per frame
        public void Update()
        {
            var handJointService = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
            var gazeProvider = CoreServices.InputSystem.EyeGazeProvider;
            TextMesh textMesh = gameObject.transform.GetChild(0).GetComponent<TextMesh>();

            // do we currently have a Viewpoint?
            GameObject instanceContainer = DistributedObjectFactory.FindFirstContainer(
                DistributedObjectFactory.DistributedType.Viewpoint);

            (Vector3 localHandPos, HandPose handPose) = LocalHandPosition(handJointService, gazeProvider, Handedness.Right);
            Vector3 viewpointHandPos = ViewpointHandPosition(instanceContainer, Handedness.Right);

            Vector3 localHeadPos = LocalHeadPosition(gazeProvider);
            Vector3 viewpointHeadPos = ViewpointHeadPosition(instanceContainer);

            float localVerticalHeadHandDistance = float.NaN;
            float localLinearHeadHandDistance = float.NaN;
            Vector3 localHeadHandVector = Vector3.zero;
            if (!IsNaN(localHandPos) && !IsNaN(localHeadPos))
            {
                localVerticalHeadHandDistance = Math.Abs(localHandPos.y - localHeadPos.y);
                localLinearHeadHandDistance = Vector3.Distance(localHandPos, localHeadPos);
                localHeadHandVector = localHandPos - localHeadPos;
            }

            float viewpointVerticalHeadHandDistance = float.NaN;
            float viewpointLinearHeadHandDistance = float.NaN;
            Vector3 viewpointHeadHandVector = Vector3.zero;
            if (!IsNaN(viewpointHandPos) && !IsNaN(viewpointHeadPos))
            {
                viewpointVerticalHeadHandDistance = Math.Abs(viewpointHandPos.y - viewpointHeadPos.y);
                viewpointLinearHeadHandDistance = Vector3.Distance(viewpointHandPos, viewpointHeadPos);
                viewpointHeadHandVector = viewpointHandPos - viewpointHeadPos;
            }

            string statusMessage = 
$@"localHandPos {localHandPos} | viewpointHandPos {viewpointHandPos}
localHeadPos {localHeadPos} | viewpointHeadPos {viewpointHeadPos}

localVHHD {localVerticalHeadHandDistance:f4} | viewpointVHHD {viewpointVerticalHeadHandDistance:f4} | delta {Math.Abs(localVerticalHeadHandDistance - viewpointVerticalHeadHandDistance):f4}
localLHHD {localLinearHeadHandDistance:f4} | viewpointLHHD {viewpointLinearHeadHandDistance:f4} | delta {Math.Abs(localLinearHeadHandDistance - viewpointLinearHeadHandDistance):f4}

handPose {handPose}";

            textMesh.text = statusMessage;
            HoloDebug.Log(statusMessage);

            bool IsNaN(Vector3 vector)
            {
                return float.IsNaN(vector.x) && float.IsNaN(vector.y) && float.IsNaN(vector.z);
            }

            (Vector3, HandPose) LocalHandPosition(
                IMixedRealityHandJointService hjs,
                IMixedRealityEyeGazeProvider gp,
                Handedness h)
            {
                if (hjs.IsHandTracked(h))
                {
                    
                    Vector3 position = handJointService.RequestJointTransform(TrackedHandJoint.Palm, h).position;
                    _classifier.Recalculate(handJointService, gazeProvider, h);
                    return (position, _classifier.GetHandPose());
                }
                else
                {
                    return (new Vector3(float.NaN, float.NaN, float.NaN), HandPose.Unknown);
                }
            }

            Vector3 LocalHeadPosition(IMixedRealityEyeGazeProvider gp)
            {
                return gp.GazeOrigin;
            }

            // Get the first Player in the first Viewpoint in the first currently connected peer.
            Player GetPlayer0(GameObject ic)
            {
                if (ic != null)
                {
                    // get the first viewpoint out of it
                    if (ic.transform.childCount > 0)
                    {
                        LocalViewpoint localViewpoint = ic.transform.GetChild(0).GetComponent<LocalViewpoint>();
                        if (localViewpoint.PlayerCount > 0)
                        {
                            Player player0 = localViewpoint.GetPlayer(0);
                            return player0;
                        }
                    }
                }

                return default(Player);
            }

            Vector3 ViewpointHandPosition(GameObject ic, Handedness h)
            {
                Player player0 = GetPlayer0(ic);
                if (!player0.IsInitialized)
                {
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                }
                return h == Handedness.Left ? player0.LeftHandPosition : player0.RightHandPosition;
            }

            Vector3 ViewpointHeadPosition(GameObject ic)
            {
                Player player0 = GetPlayer0(ic);
                if (!player0.IsInitialized)
                {
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                }
                return player0.HeadPosition;
            }
        }
    }
}
