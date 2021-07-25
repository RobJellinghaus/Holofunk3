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
            LocalViewpoint localViewpoint = DistributedObjectFactory.FindFirstInstanceComponent<LocalViewpoint>(
                DistributedObjectFactory.DistributedType.Viewpoint);
            Player player = localViewpoint.PlayerCount > 0 ? localViewpoint.GetPlayer(0) : default(Player);

            // we know we have a Performer
            GameObject performerContainer = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Performer);
            LocalPerformer localPerformer = performerContainer.GetComponent<LocalPerformer>();

            Vector3 viewpointSensorPos = player.SensorPosition;

            Vector3 localHandPos = localPerformer.GetPerformer().RightHandPosition;
            Vector3 viewpointHandPos = player.RightHandPosition;

            HandPoseValue handPoseValue = localPerformer.GetPerformer().RightHandPose;

            Vector3 localHeadPos = localPerformer.GetPerformer().HeadPosition;
            Vector3 viewpointHeadPos = player.HeadPosition;
            Vector3 viewpointHeadForwardDir = player.HeadForwardDirection;

            float localVerticalHeadDistance = Math.Abs(localHandPos.y - localHeadPos.y);
            float localLinearHeadDistance = Vector3.Distance(localHandPos, localHeadPos);

            float viewpointVerticalHeadDistance = Math.Abs(viewpointHandPos.y - viewpointHeadPos.y);
            float viewpointLinearHeadDistance = Vector3.Distance(viewpointHandPos, viewpointHeadPos);
            Vector3 headToViewpointVector = (viewpointSensorPos - viewpointHeadPos).normalized;

            string statusMessage =
$@"
viewpointSensorPos {viewpointSensorPos}
viewpointForwardDir {player.SensorForwardDirection}
localHandPos {localHandPos} | viewpointHandPos {viewpointHandPos}
localHeadPos {localHeadPos} | viewpointHeadPos {viewpointHeadPos}

headforward-> {viewpointHeadForwardDir} | headtosensor-> {headToViewpointVector} 
| collin {Vector3.Dot(viewpointHeadForwardDir, headToViewpointVector):f4} 

Head:
localvertdist {localVerticalHeadDistance:f4} | viewpointvertdist {viewpointVerticalHeadDistance:f4} | delta {Math.Abs(localVerticalHeadDistance - viewpointVerticalHeadDistance):f4}
localdist {localLinearHeadDistance:f4} | viewpointdist {viewpointLinearHeadDistance:f4} | delta {Math.Abs(localLinearHeadDistance - viewpointLinearHeadDistance):f4}

handPose {handPoseValue}";

            textMesh.text = statusMessage;

            if (player.PlayerId != default(PlayerId))
            {
                if (player.ViewpointToPerformerMatrix != Matrix4x4.zero)
                {
                    // position the "FloatingTextPanel 2" at the transformed position of the sensor
                    Vector3 sensorPosition = player.SensorPosition;
                    Vector3 sensorPositionInPerformerSpace = player.ViewpointToPerformerMatrix.MultiplyPoint(sensorPosition);

                    GameObject panel2 = GameObject.Find("FloatingTextPanel 2");
                    panel2.transform.position = sensorPositionInPerformerSpace;
                }
            }
        }

        private static Player GetPlayer0(GameObject ic)
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
