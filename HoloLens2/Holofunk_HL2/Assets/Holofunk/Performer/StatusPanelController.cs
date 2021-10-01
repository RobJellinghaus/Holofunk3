/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.HandComponents;
using Holofunk.Loop;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Linq;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// This behavior expects to inhabit a "FloatingTextPanel" GameObject.
    /// </summary>
    public class StatusPanelUpdater : MonoBehaviour
    {
        private HandPoseClassifier _classifier = new HandPoseClassifier();

        private bool showDistanceStatistics = true;

        private bool showRightHandProperties = false;

        private bool showLoopieDistance = false;

        // Update is called once per frame
        public void Update()
        {
            TextMesh textMesh = gameObject.transform.GetChild(0).GetComponent<TextMesh>();

            // do we currently have a Viewpoint?
            LocalViewpoint localViewpoint = DistributedObjectFactory.FindFirstInstanceComponent<LocalViewpoint>(
                DistributedObjectFactory.DistributedType.Viewpoint);
            // if not, then nothing to do
            if (localViewpoint == null)
            {
                return;
            }

            PlayerState player = localViewpoint.PlayerCount > 0 ? localViewpoint.GetPlayer(0) : default(PlayerState);

            // we know we have a Performer
            GameObject performerContainer = DistributedObjectFactory.FindPrototypeContainer(DistributedObjectFactory.DistributedType.Performer);
            LocalPerformer localPerformer = performerContainer.GetComponent<LocalPerformer>();

            if (showDistanceStatistics)
            {
                ShowDistanceStatisticsInTextPanel(textMesh, player, localPerformer);
            }

            if (showRightHandProperties)
            {
                ShowRightHandPropertiesInTextPanel(textMesh, player, localPerformer);
            }

            if (showLoopieDistance)
            {
                ShowLoopieDistanceInTextPanel(textMesh, player, localPerformer);
            }
        }

        private void ShowLoopieDistanceInTextPanel(TextMesh textMesh, PlayerState player, LocalPerformer localPerformer)
        {
            LocalLoopie firstLoopie = DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false).FirstOrDefault();
            if (firstLoopie == null)
            {
                textMesh.text = $@"
player.PlayerId: {player.PlayerId}
player.Initialized: {player.PlayerId.IsInitialized}

No loopies yet";
            }
            else
            {
                Vector3 handPos = localPerformer.GetPerformer().RightHandPosition;
                Vector3 loopiePos = firstLoopie.transform.position;
                textMesh.text = $@"
player.PlayerId: {player.PlayerId}
player.Initialized: {player.PlayerId.IsInitialized}

Hand position: {handPos}
Loopie position: {loopiePos}
Distance: {Vector3.Distance(handPos, loopiePos)}";
            }
        }

        private void ShowRightHandPropertiesInTextPanel(TextMesh textMesh, PlayerState player, LocalPerformer localPerformer)
        {
            HandPoseValue rightHandPose = localPerformer.GetPerformer().RightHandPose;
            HandController rightHandController = localPerformer
                .gameObject
                .transform
                .GetChild(1)
                .GetComponent<HandController>();

            textMesh.text =
$@"Player's host address {player.PerformerHostAddress}
Right hand pose {rightHandPose}
Right hand state {rightHandController.HandStateMachineInstanceString}";
        }

        private static void ShowDistanceStatisticsInTextPanel(TextMesh text, PlayerState p, LocalPerformer performer)
        {
            Vector3 viewpointSensorPos = p.SensorPosition;

            Vector3 localHandPos = performer.GetPerformer().RightHandPosition;
            Vector3 viewpointHandPos = p.RightHandPosition;

            HandPoseValue handPoseValue = performer.GetPerformer().RightHandPose;

            Vector3 localHeadPos = performer.GetPerformer().HeadPosition;
            Vector3 viewpointHeadPos = p.HeadPosition;
            Vector3 viewpointHeadForwardDir = p.HeadForwardDirection;

            float localVerticalHeadDistance = Math.Abs(localHandPos.y - localHeadPos.y);
            float localLinearHeadDistance = Vector3.Distance(localHandPos, localHeadPos);

            float viewpointVerticalHeadDistance = Math.Abs(viewpointHandPos.y - viewpointHeadPos.y);
            float viewpointLinearHeadDistance = Vector3.Distance(viewpointHandPos, viewpointHeadPos);
            Vector3 headToViewpointVector = (viewpointSensorPos - viewpointHeadPos).normalized;

            string statusMessage =
$@"
viewpointSensorPos {viewpointSensorPos}
viewpointForwardDir {p.SensorForwardDirection}
localHandPos {localHandPos} | viewpointHandPos {viewpointHandPos}
localHeadPos {localHeadPos} | viewpointHeadPos {viewpointHeadPos}

headforward-> {viewpointHeadForwardDir} | headtosensor-> {headToViewpointVector} 
| collin {Vector3.Dot(viewpointHeadForwardDir, headToViewpointVector):f4} 

Head:
localvertdist {localVerticalHeadDistance:f4} | viewpointvertdist {viewpointVerticalHeadDistance:f4} | delta {Math.Abs(localVerticalHeadDistance - viewpointVerticalHeadDistance):f4}
localdist {localLinearHeadDistance:f4} | viewpointdist {viewpointLinearHeadDistance:f4} | delta {Math.Abs(localLinearHeadDistance - viewpointLinearHeadDistance):f4}

handPose {handPoseValue}";

            text.text = statusMessage;
        }

        private static PlayerState GetPlayer0(GameObject ic)
        {
            if (ic != null)
            {
                // get the first viewpoint out of it
                if (ic.transform.childCount > 0)
                {
                    LocalViewpoint localViewpoint = ic.transform.GetChild(0).GetComponent<LocalViewpoint>();
                    if (localViewpoint.PlayerCount > 0)
                    {
                        PlayerState player0 = localViewpoint.GetPlayer(0);
                        return player0;
                    }
                }
            }

            return default(PlayerState);
        }
    }
}
