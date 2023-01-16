// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Serialized state of a player (e.g. a recognized/tracked individual) as seen from a viewpoint;
    /// all coordinates are in viewpoint space.
    /// </summary>
    /// <remarks>
    /// If a joint is not currently tracked, all values for that joint will be zero.
    /// </remarks>
    public struct PlayerState : INetSerializable
    {
        /// <summary>
        /// Player identifier from the Viewpoint.
        /// </summary>
        public PlayerId PlayerId { get; set; }

        /// <summary>
        /// The Kinect user ID.
        /// </summary>
        /// <remarks>
        /// This is not really tracked properly yet. TODO: track userid properly.
        /// </remarks>
        public UserId UserId { get; set; }

        /// <summary>
        /// Is this player currently tracked?
        /// </summary>
        public bool Tracked { get; set; }

        /// <summary>
        /// If we know which Performer this is, this is the address of that Performer's Host.
        /// </summary>
        /// <remarks>
        /// This is the key means by which the viewpoint arbitrates which other coordinate space
        /// it believes this player is hosted in.
        /// 
        /// Right now we support only one performer per remote host address.
        /// </remarks>
        public SerializedSocketAddress PerformerHostAddress { get; set; }

        /// <summary>
        /// The sensor (viewpoint) position, in viewpoint coordinates.
        /// 
        /// Commented out because the Kinect V2 does not seem to support this.
        /// </summary>
        //public Vector3 SensorPosition { get; set; }

        /// <summary>
        /// The sensor (viewpoint) forward direction, in viewpoint coordinates.
        /// </summary>
        /// <remarks>
        /// This is the sensor orientation multiplied by a unit Z vector (e.g. (0, 0, 1)).
        /// 
        /// Commented out because the Kinect V2 does not seem to support this.
        /// </remarks>
        //public Vector3 SensorForwardDirection { get; set; }

        /// <summary>
        /// The position of the head, in viewpoint coordinates.
        /// </summary>
        public Vector3 HeadPosition { get; set; }

        /// <summary>
        /// The forward direction of the head, in viewpoint coordinates.
        /// </summary>
        public Vector3 HeadForwardDirection { get; set; }

        /// <summary>
        /// The head position at the moment that the head forward direction was most aligned
        /// with the head->sensor direction.
        /// </summary>
        /// <remarks>
        /// This is populated when the system is tracking players before recognizing them;
        /// we want to base the transformation on the moment they were looking most directly at
        /// the sensor.
        /// </remarks>
        public Vector3 MostSensorAlignedHeadPosition { get; set; }

        /// <summary>
        /// The head forward direction that was most aligned with the head->sensor direction.
        /// </summary>
        /// <remarks>
        /// This is populated when the system is tracking players before recognizing them;
        /// we want to base the transformation on the moment they were looking most directly at
        /// the sensor.
        /// </remarks>
        public Vector3 MostSensorAlignedHeadForwardDirection { get; set; }

        /// <summary>
        /// The value of the dot product between MostSensorAlignedHeadForwardDirection and
        /// the head->sensor direction, at the time that MostSensorAlignedHeadForwardDirection
        /// was set.
        /// </summary>
        /// <remarks>
        /// This is the computed value we want the maximum value of, when making the player-performer
        /// coordinate mapping.
        /// </remarks>
        public float MostSensorAlignment { get; set; }

        /// <summary>
        /// The position of the left hand, in viewpoint coordinates.
        /// </summary>
        public Vector3 LeftHandPosition { get; set; }

        /// <summary>
        /// The position of the right hand, in viewpoint coordinates.
        /// </summary>
        public Vector3 RightHandPosition { get; set; }

        /// <summary>
        /// The transformation matrix from performer space to viewpoint space (e.g. the local-to-world matrix,
        /// considering the viewpoint as the world).
        /// </summary>
        public Matrix4x4 PerformerToViewpointMatrix { get; set; }

        /// <summary>
        /// The inverse of the PerformerToViewpointMatrix.
        /// </summary>
        public Matrix4x4 ViewpointToPerformerMatrix { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = PlayerId.Deserialize(reader);
            UserId = UserId.Deserialize(reader);
            Tracked = reader.GetBool();
            PerformerHostAddress = SerializedSocketAddress.Deserialize(reader);
            //SensorPosition = reader.GetVector3();
            //SensorForwardDirection = reader.GetVector3();
            HeadPosition = reader.GetVector3();
            HeadForwardDirection = reader.GetVector3();
            MostSensorAlignedHeadPosition = reader.GetVector3();
            MostSensorAlignedHeadForwardDirection = reader.GetVector3();
            LeftHandPosition = reader.GetVector3();
            RightHandPosition = reader.GetVector3();
            PerformerToViewpointMatrix = reader.GetMatrix4x4();
            ViewpointToPerformerMatrix = reader.GetMatrix4x4();
        }

        public void Serialize(NetDataWriter writer)
        {
            PlayerId.Serialize(writer, PlayerId);
            UserId.Serialize(writer, UserId);
            writer.Put(Tracked);
            SerializedSocketAddress.Serialize(writer, PerformerHostAddress);
            //writer.Put(SensorPosition);
            //writer.Put(SensorForwardDirection);
            writer.Put(HeadPosition);
            writer.Put(HeadForwardDirection);
            writer.Put(MostSensorAlignedHeadPosition);
            writer.Put(MostSensorAlignedHeadForwardDirection);
            writer.Put(LeftHandPosition);
            writer.Put(RightHandPosition);
            writer.Put(PerformerToViewpointMatrix);
            writer.Put(ViewpointToPerformerMatrix);
        }
    }
}
