﻿/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

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
    public struct Player : INetSerializable
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
        /// </summary>
        public Vector3 SensorPosition { get; set; }

        /// <summary>
        /// The sensor (viewpoint) forward direction, in viewpoint coordinates.
        /// </summary>
        /// <remarks>
        /// This is the sensor orientation multiplied by a unit Z vector (e.g. (0, 0, 1)).
        /// </remarks>
        public Vector3 SensorForwardDirection { get; set; }

        /// <summary>
        /// The position of the head, in viewpoint coordinates.
        /// </summary>
        public Vector3 HeadPosition { get; set; }

        /// <summary>
        /// The forward direction of the head, in viewpoint coordinates.
        /// </summary>
        public Vector3 HeadForwardDirection { get; set; }

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
        public Matrix4x4 PerformerToViewpointTransform { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = PlayerId.Deserialize(reader);
            UserId = UserId.Deserialize(reader);
            Tracked = reader.GetBool();
            PerformerHostAddress.Deserialize(reader);
            SensorPosition = reader.GetVector3();
            SensorForwardDirection = reader.GetVector3();
            HeadPosition = reader.GetVector3();
            HeadForwardDirection = reader.GetVector3();
            LeftHandPosition = reader.GetVector3();
            RightHandPosition = reader.GetVector3();
            PerformerToViewpointTransform = reader.GetMatrix4x4();
        }

        public void Serialize(NetDataWriter writer)
        {
            PlayerId.Serialize(writer, PlayerId);
            UserId.Serialize(writer, UserId);
            writer.Put(Tracked);
            PerformerHostAddress.Serialize(writer);
            writer.Put(SensorPosition);
            writer.Put(SensorForwardDirection);
            writer.Put(HeadPosition);
            writer.Put(HeadForwardDirection);
            writer.Put(LeftHandPosition);
            writer.Put(RightHandPosition);
            writer.Put(PerformerToViewpointTransform);
        }
    }
}
