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

                                        // The point of this code is to calculate the transformation matrices between viewpoint
                                        // space and performer space, using the performer's head as the origin of the conversion.
                                        // In other words the performer's head, at the moment of performer recognition, becomes
                                        // the "anchor" that both systems have a common view of.

                                        // We rely on both coordinate spaces having close enough scale that we can ignore scaling
                                        // transformation. We also rely on them having consistent and flat Y planes, so the only
                                        // rotational transformation we need is about the Y (up) axis. We call this "flat-rotation".

                                        // The transformation from performer space to viewpoint space is:
                                        // - Subtract the performer's head position
                                        //   (e.g. bring the performer's head position to the origin)
                                        // - Flat-rotate from the performer's forward direction to a straight unit Z direction
                                        //   (e.g. bring the performer's orientation to the origin)
                                        // - Flat-rotate from the sensor forward direction to the player's head's forward direction
                                        //   (e.g. rotate to the head orientation in viewpoint space)
                                        // - Add the player's head position
                                        //   (e.g. translate to the head position in viewpoint space)

                                        Vector3 Flatten(Vector3 v) => new Vector3(v.x, 0, v.z);

                                        // Translate from the performer's head location to the performance's origin.
                                        Matrix4x4 translateFromPerformerHeadMatrix = Matrix4x4.Translate(-performance.HeadPosition);

                                        // Flat-rotate from the performer's gaze direction to the unit Z direction.
                                        Matrix4x4 flatRotateFromPerformerHeadMatrix = Matrix4x4.Rotate(
                                            Quaternion.FromToRotation(Flatten(performance.HeadForwardDirection), Vector3.forward));

                                        // Flat-rotate from the unit Z direction to the viewpoint's head direction.
                                        Matrix4x4 flatRotateToPlayerHeadMatrix = Matrix4x4.Rotate(
                                            Quaternion.FromToRotation(Vector3.forward, Flatten(player.HeadForwardDirection)));

                                        // Translate to the viewpoint's head position.
                                        Matrix4x4 translateToPlayerHeadMatrix = Matrix4x4.Translate(player.HeadPosition);

                                        Vector3 point1_0 = Vector3.forward;
                                        Vector3 point1_1 = translateFromPerformerHeadMatrix.MultiplyPoint(point1_0);
                                        Vector3 point1_2 = flatRotateFromPerformerHeadMatrix.MultiplyPoint(point1_1);
                                        Vector3 point1_3 = flatRotateToPlayerHeadMatrix.MultiplyPoint(point1_2);
                                        Vector3 point1_4 = translateToPlayerHeadMatrix.MultiplyPoint(point1_3);

                                        Matrix4x4 finalMatrix = translateToPlayerHeadMatrix
                                            * flatRotateToPlayerHeadMatrix
                                            * flatRotateFromPerformerHeadMatrix
                                            * translateFromPerformerHeadMatrix;
                                        Vector3 finalPosition = finalMatrix.MultiplyPoint(Vector3.forward);

                                        Matrix4x4 translateFromPlayerHeadMatrix = Matrix4x4.Translate(performance.HeadPosition);
                                        Matrix4x4 rotateFromPlayerHeadMatrix =
                                            Matrix4x4.Rotate(Quaternion.FromToRotation(Flatten(player.HeadForwardDirection), Vector3.forward));
                                        Matrix4x4 rotateToPerformerHeadMatrix =
                                            Matrix4x4.Rotate(Quaternion.FromToRotation(Vector3.forward, Flatten(performance.HeadForwardDirection)));
                                        Matrix4x4 translateToPerformerHeadMatrix =
                                            Matrix4x4.Translate(performance.HeadPosition);

                                        Vector3 point2_0 = Vector3.forward;
                                        Vector3 point2_1 = translateFromPlayerHeadMatrix.MultiplyPoint(point2_0);
                                        Vector3 point2_2 = rotateFromPlayerHeadMatrix.MultiplyPoint(point2_1);
                                        Vector3 point2_3 = rotateToPerformerHeadMatrix.MultiplyPoint(point2_2);
                                        Vector3 point2_4 = translateToPerformerHeadMatrix.MultiplyPoint(point2_3);

                                        Matrix4x4 finalInverseMatrix =
                                            translateToPerformerHeadMatrix
                                            * rotateToPerformerHeadMatrix
                                            * rotateFromPlayerHeadMatrix
                                            * translateFromPlayerHeadMatrix;

                                        player.PerformerToViewpointTransform = finalMatrix;
                                        player.ViewpointToPerformerTransform = finalInverseMatrix;

                                        HoloDebug.Log($@"Recognized new player!
viewpointSensorPosition {viewpointSensorPosition}, viewpointSensorForwardDirection {viewpointSensorForwardDirection}
viewpointHeadPosition {viewpointHeadPosition}, viewpointHeadForwardDirection {viewpointHeadForwardDirection}

Performer {point1_0} => {point1_1} => {point1_2} => {point1_3} => {point1_4} Viewpoint
Or: => {finalPosition}

Viewpoint {point2_0} => {point2_1} => {point2_2} => {point2_3} => {point2_4} Performer
Or: => {finalInverseMatrix.MultiplyPoint(Vector3.forward)}
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
