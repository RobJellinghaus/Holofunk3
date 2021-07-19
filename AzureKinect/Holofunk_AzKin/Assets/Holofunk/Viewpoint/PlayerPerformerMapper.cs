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
                        Vector3 viewpointSensorForwardDirection = player.SensorForwardDirection;

                        Vector3 headToViewpointVector = (viewpointSensorPosition - viewpointHeadPosition).normalized;
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
                                    var owningPeerAddress = new SerializedSocketAddress(thePerformer.OwningPeer);
                                    if (player.PerformerHostAddress == owningPeerAddress)
                                    {
                                        HoloDebug.Log($"Recognized known performer at address {player.PerformerHostAddress}!");
                                    }
                                    else
                                    {
                                        // you're the one for us
                                        player.PerformerHostAddress = new SerializedSocketAddress(thePerformer.OwningPeer);

                                        // We rely on both coordinate spaces having close enough scale that we can ignore scaling
                                        // transformation. We also rely on them having consistent and flat Y planes, so the only
                                        // rotational transformation we need is about the Y (up) axis.

                                        // Rotate from the performer's look vector to the camera's look vector.

                                        // Rotate performer vector to viewpoint vector about Y axis only
                                        Quaternion performerToViewpointRotation = Quaternion.FromToRotation(
                                            viewpointHeadForwardDirection,
                                            viewpointSensorForwardDirection);
                                        Quaternion viewpointToPerformerRotation = Quaternion.FromToRotation(
                                            viewpointSensorForwardDirection,
                                            viewpointHeadForwardDirection);
                                        Quaternion performerToViewpointFlattenedRotation = Quaternion.FromToRotation(
                                            new Vector3(viewpointHeadForwardDirection.x, 0, viewpointHeadForwardDirection.z),
                                            new Vector3(viewpointSensorForwardDirection.x, 0, viewpointSensorForwardDirection.z));

                                        // TODO TOMORROW: are the above values the same? HYPOTHESIS: YES. CONCLUSION: YES.
                                        Matrix4x4 performerToViewpointRotationMatrix = Matrix4x4.Rotate(performerToViewpointRotation);
                                        Matrix4x4 performerToViewpointFlattenedRotationMatrix = Matrix4x4.Rotate(performerToViewpointFlattenedRotation);

                                        Matrix4x4 viewpointToPerformerRotationMatrix = Matrix4x4.Rotate(viewpointToPerformerRotation);

                                        Vector3 rotationEulerAngles = performerToViewpointRotation.eulerAngles;
                                        Vector3 flattenedRotationEulerAngles = performerToViewpointFlattenedRotation.eulerAngles;

                                        // Translation to viewpoint-relative location
                                        Matrix4x4 viewpointTranslation = Matrix4x4.Translate(viewpointHeadPosition);

                                        Vector3 pointInFrontOfMe = new Vector3(0, 0, 1);
                                        Vector3 rotatedPosition = performerToViewpointRotationMatrix.MultiplyPoint(pointInFrontOfMe);
                                        Vector3 viewpointPosition = viewpointTranslation.MultiplyPoint(rotatedPosition);

                                        Vector3 flattenedRotatedPosition = performerToViewpointFlattenedRotationMatrix.MultiplyPoint(pointInFrontOfMe);
                                        Vector3 flattenedViewpointPosition = viewpointTranslation.MultiplyPoint(flattenedRotatedPosition);

                                        Matrix4x4 finalMatrix = viewpointTranslation * performerToViewpointRotationMatrix;
                                        Vector3 finalPosition = finalMatrix.MultiplyPoint(new Vector3(0, 0, 1));

                                        player.PerformerToViewpointTransform = finalMatrix;

                                        Matrix4x4 finalInverseMatrix = viewpointToPerformerRotationMatrix * Matrix4x4.Translate(-viewpointHeadPosition);
                                        player.ViewpointToPerformerTransform = finalInverseMatrix;

                                        HoloDebug.Log($@"Recognized new player!
viewpointSensorPosition {viewpointSensorPosition}, viewpointSensorForwardDirection {viewpointSensorForwardDirection}
viewpointHeadPosition {viewpointHeadPosition}, viewpointHeadForwardDirection {viewpointHeadForwardDirection}
rotationEulerAngles {rotationEulerAngles}, flatEulerAngles {flattenedRotationEulerAngles}
Z vector rotated to viewpoint alignment: {rotatedPosition}
Z vector transformed to viewpoint location: {viewpointPosition}
Z vector transformed by final matrix: {finalPosition}
Z vector flat-rotated to viewpoint forward dir: {flattenedRotatedPosition}
Z vector flat-transformed to viewpoint location: {flattenedViewpointPosition}
");

                                        theViewpoint.UpdatePlayer(player);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
