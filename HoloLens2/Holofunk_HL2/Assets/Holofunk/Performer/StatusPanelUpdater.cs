/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.HandPose;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Holofunk.Performer
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

            // we know we have a Performer
            GameObject performerContainer = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Performer);
            LocalPerformer localPerformer = performerContainer.GetComponent<LocalPerformer>();

            Vector3 localHandPos = localPerformer.GetPerformer().RightHandPosition;
            Vector3 viewpointHandPos = ViewpointHandPosition(instanceContainer, Handedness.Right);

            HandPoseValue handPoseValue = localPerformer.GetPerformer().RightHandPose;

            Vector3 localHeadPos = localPerformer.GetPerformer().HeadPosition;
            Vector3 viewpointHeadPos = ViewpointHeadPosition(instanceContainer);
            Vector3 viewpointAverageEyesPos = ViewpointAverageEyesPosition(instanceContainer);

            float localVerticalHeadDistance = Math.Abs(localHandPos.y - localHeadPos.y);
            float localLinearHeadDistance = Vector3.Distance(localHandPos, localHeadPos);

            float viewpointVerticalHeadDistance = Math.Abs(viewpointHandPos.y - viewpointHeadPos.y);
            float viewpointLinearHeadDistance = Vector3.Distance(viewpointHandPos, viewpointHeadPos);

            float viewpointVerticalEyesDistance = Math.Abs(viewpointHandPos.y - viewpointAverageEyesPos.y);
            float viewpointLinearEyesDistance = Vector3.Distance(viewpointHandPos, viewpointAverageEyesPos);

            string statusMessage = 
$@"localHandPos {localHandPos} | viewpointHandPos {viewpointHandPos}
localHeadPos {localHeadPos} | viewpointHeadPos {viewpointHeadPos}
localHeadPos {localHeadPos} | viewpointAverageEyesPos {viewpointAverageEyesPos}

Local:
localvertdist {localVerticalHeadDistance:f4} | viewpointvertdist {viewpointVerticalHeadDistance:f4} | delta {Math.Abs(localVerticalHeadDistance - viewpointVerticalHeadDistance):f4}
localdist {localLinearHeadDistance:f4} | viewpointdist {viewpointLinearHeadDistance:f4} | delta {Math.Abs(localLinearHeadDistance - viewpointLinearHeadDistance):f4}

Eyes:
localvertdist {localVerticalHeadDistance:f4} | viewpointeyesvertdist {viewpointVerticalEyesDistance:f4} | delta {Math.Abs(localVerticalHeadDistance - viewpointVerticalEyesDistance):f4}
localdist {localLinearHeadDistance:f4} | viewpointeyesdist {viewpointLinearEyesDistance:f4} | delta {Math.Abs(localLinearHeadDistance - viewpointLinearEyesDistance):f4}

handPose {handPoseValue}";

            textMesh.text = statusMessage;
            //HoloDebug.Log(statusMessage);

            bool IsNaN(Vector3 vector)
            {
                return float.IsNaN(vector.x) && float.IsNaN(vector.y) && float.IsNaN(vector.z);
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
                if (!player0.PlayerId.IsInitialized)
                {
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                }
                return h == Handedness.Left ? player0.LeftHandPosition : player0.RightHandPosition;
            }

            Vector3 ViewpointHeadPosition(GameObject ic)
            {
                Player player0 = GetPlayer0(ic);
                if (!player0.PlayerId.IsInitialized)
                {
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                }
                return player0.HeadPosition;
            }

            Vector3 ViewpointAverageEyesPosition(GameObject ic)
            {
                Player player0 = GetPlayer0(ic);
                if (!player0.PlayerId.IsInitialized)
                {
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                }
                return player0.AverageEyesPosition;
            }
        }
    }
}
