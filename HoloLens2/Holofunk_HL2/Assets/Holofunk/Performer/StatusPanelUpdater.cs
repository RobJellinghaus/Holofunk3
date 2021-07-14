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
            TextMesh textMesh = gameObject.transform.GetChild(0).GetComponent<TextMesh>();

            // do we currently have a Viewpoint?
            GameObject instanceContainer = DistributedObjectFactory.FindFirstContainer(
                DistributedObjectFactory.DistributedType.Viewpoint);
            Player player = GetPlayer0(instanceContainer);

            // we know we have a Performer
            GameObject performerContainer = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Performer);
            LocalPerformer localPerformer = performerContainer.GetComponent<LocalPerformer>();

            Vector3 viewpointSensorPos = player.ViewpointPosition;

            Vector3 localHandPos = localPerformer.GetPerformer().RightHandPosition;
            Vector3 viewpointHandPos = player.RightHandPosition;

            HandPoseValue handPoseValue = localPerformer.GetPerformer().RightHandPose;

            Vector3 localHeadPos = localPerformer.GetPerformer().HeadPosition;
            Vector3 viewpointHeadPos = player.HeadPosition;
            Vector3 viewpointHeadForwardDir = player.HeadForwardDirection;
            Vector3 viewpointAverageEyesPos = player.AverageEyesPosition;
            Vector3 viewpointAverageEyesForwardDir = player.AverageEyesForwardDirection;

            float localVerticalHeadDistance = Math.Abs(localHandPos.y - localHeadPos.y);
            float localLinearHeadDistance = Vector3.Distance(localHandPos, localHeadPos);

            float viewpointVerticalHeadDistance = Math.Abs(viewpointHandPos.y - viewpointHeadPos.y);
            float viewpointLinearHeadDistance = Vector3.Distance(viewpointHandPos, viewpointHeadPos);

            float viewpointVerticalEyesDistance = Math.Abs(viewpointHandPos.y - viewpointAverageEyesPos.y);
            float viewpointLinearEyesDistance = Vector3.Distance(viewpointHandPos, viewpointAverageEyesPos);

            Vector3 eyesToViewpointVector = (viewpointAverageEyesPos - viewpointSensorPos).normalized;
            Vector3 headToViewpointVector = (viewpointHeadPos - viewpointSensorPos).normalized;

            string statusMessage = 
$@"
viewpointSensorPos {viewpointSensorPos} 
localHandPos {localHandPos} | viewpointHandPos {viewpointHandPos}
localHeadPos {localHeadPos} | viewpointHeadPos {viewpointHeadPos}
localHeadPos {localHeadPos} | viewpointAverageEyesPos {viewpointAverageEyesPos}

eyesorientation-> {viewpointAverageEyesForwardDir} | eyestosensor-> {eyesToViewpointVector} | collin {Vector3.Dot(viewpointAverageEyesForwardDir, eyesToViewpointVector):f4} 
viewpointHead-> {viewpointHeadForwardDir} | headtosensor-> {headToViewpointVector} | collin {Vector3.Dot(viewpointHeadForwardDir, headToViewpointVector):f4} 

Head:
localvertdist {localVerticalHeadDistance:f4} | viewpointvertdist {viewpointVerticalHeadDistance:f4} | delta {Math.Abs(localVerticalHeadDistance - viewpointVerticalHeadDistance):f4}
localdist {localLinearHeadDistance:f4} | viewpointdist {viewpointLinearHeadDistance:f4} | delta {Math.Abs(localLinearHeadDistance - viewpointLinearHeadDistance):f4}

Eyes:
localvertdist {localVerticalHeadDistance:f4} | viewpointvertdist {viewpointVerticalEyesDistance:f4} | delta {Math.Abs(localVerticalHeadDistance - viewpointVerticalEyesDistance):f4}
localdist {localLinearHeadDistance:f4} | viewpointeyesdist {viewpointLinearEyesDistance:f4} | delta {Math.Abs(localLinearHeadDistance - viewpointLinearEyesDistance):f4}

handPose {handPoseValue}";

            textMesh.text = statusMessage;
            //HoloDebug.Log(statusMessage);

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
        }
    }
}
