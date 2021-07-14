/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using com.rfilkov.kinect;
using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Perform;
using LiteNetLib;
using System.Collections.Generic;
using UnityEngine;
using static Holofunk.Viewpoint.ViewpointMessages;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Determines which Players map to which Performers.
    /// </summary>
    /// <remarks>
    /// The Player is the representation of a detected person from the Kinect's viewpoint.
    /// The Performer is the representation of a person wearing a HoloLens device.
    /// The question is, which Player is which Performer?
    /// 
    /// The system expresses its knowledge of which Player is which Performer based on the
    /// Player.PerformerHostAddress property; right now we assume each Performer has their
    /// own IP address (correct assumption for initial rollout).
    /// 
    /// This PlayerPerformerMapper behaviour is responsible for assigning the PerformerHostAddress
    /// of a given Player, once it determines that Performer is the same person as that Player.
    /// 
    /// The algorithm for computing this:
    /// - On each frame:
    ///   - Look for Players matching the following qualities:
    ///     - Looking at the camera
    ///     - Raising one hand only
    ///       - in front of them (closer to the camera)
    ///       - at close to eye level
    ///   - Look for Performers matching the following condition:
    ///     - One hand in view
    ///     - Hand pose open
    ///     - Hand at close to eye level
    ///   - If there is more than one Player and/or Performer matching these, then
    ///     (eventually) ask for there to be only one (somehow)
    ///   - Otherwise:
    ///     - Set that Player's PerformerHostAddress to match the Performer's host address
    ///     
    /// TODO: Unset the PerformerHostAddress if the Performer disconnects.
    /// 
    /// Note that this behaviour is stateless itself.
    /// </remarks>
    public class PlayerPerformerMapper : MonoBehaviour
    {
        /// <summary>
        /// Reused list of player IDs, for detecting candidate "hands-up" players
        /// </summary>
        private List<int> playerList = new List<int>();

        public void Update()
        {
            DistributedViewpoint theViewpoint = Viewpoint.TheInstance;

            // Any tracked but unidentified players?
            bool trackedButUnidentified = false;
            for (int i = 0; i < theViewpoint.PlayerCount; i++)
            {
                Player player = theViewpoint.GetPlayer(i);
                if (player.Tracked && player.PerformerHostAddress == default(SerializedSocketAddress))
                {
                    trackedButUnidentified = true;
                    break;
                }
            }

            if (!trackedButUnidentified)
            {
                // we know who everyone is
                return;
            }

            KinectManager kinectManager = KinectManager.Instance;
            playerList.Clear();

            if (kinectManager.IsInitialized())
            {
                // Look for players that are raising one hand and looking at the Kinect
                for (int i = 0; i < theViewpoint.PlayerCount; i++)
                {
                    Player player = theViewpoint.GetPlayer(i);
                    if (player.Tracked && player.PerformerHostAddress == default(SerializedSocketAddress))
                    {
                        // Are the player and the camera looking at each other?
                        // Determine this by computing the dot product) between the player's normalized gaze ray and
                        // the player's head-to-sensor ray.
                        Vector3 viewpointHeadPosition = player.HeadPosition;
                        Vector3 viewpointHeadForwardDirection = player.HeadForwardDirection;
                        Vector3 viewpointSensorPosition = player.SensorPosition;

                        Vector3 headToViewpointVector = (viewpointHeadPosition - viewpointSensorPosition).normalized;
                        float gazeViewpointAlignment = Vector3.Dot(headToViewpointVector, viewpointHeadForwardDirection);

                        if (gazeViewpointAlignment > MagicNumbers.MinimumGazeViewpointAlignment)
                        {
                            // This player is indeed looking towards the camera.

                            // Is their hand at a vertical (Y) level close to their head?
                            // TODO: even care about that at all (skip for now)

                            // Do we have a performer?
                            // TODO: support multiple performers. (be nice to have multiple HoloLenses LOL)
                            // In a multiple-performer world, each of these filters applies to each player/performer;
                            // for now we only look for one performer.
                            if (Viewpoint.GetPerformerCount() > 0)
                            {
                                DistributedPerformer thePerformer = Viewpoint.GetPerformer(0);
                                // is their hand open?
                                Performer performance = thePerformer.GetPerformer();

                                if (performance.LeftHandPose == HandPoseValue.Opened
                                    || performance.RightHandPose == HandPoseValue.Opened)
                                {
                                    // you're the one for us
                                    player.PerformerHostAddress = new SerializedSocketAddress(thePerformer.OwningPeer);
                                    theViewpoint.UpdatePlayer(player);

                                    // Now, calculate the actual transforms between performer coordinates and viewpoint coordinates.
                                    // First, which hand was it? If both were opened, prefer right hand.
                                    KinectInterop.JointType whichHand;
                                    if (performance.RightHandPose == HandPoseValue.Opened)
                                    {
                                        whichHand = KinectInterop.JointType.HandRight;
                                    }
                                    else
                                    {
                                        whichHand = KinectInterop.JointType.HandLeft;
                                    }

                                    Vector3 viewpointHandPosition = kinectManager.GetJointPosition((ulong)player.UserId, whichHand);

                                    HoloDebug.Log($"I see player at address {player.PerformerHostAddress}!!!");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
