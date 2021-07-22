// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Distributed;
using Holofunk.Hand;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Loopie
{
    /// <summary>
    /// Serialized state of a performer (e.g. a person wearing a HoloLens 2).
    /// </summary>
    /// <remarks>
    /// If a joint is not currently tracked, all values for that joint will be zero.
    /// </remarks>
    public struct Loopie : INetSerializable
    {
        /// <summary>
        /// The position of the head, in performer coordinates.
        /// </summary>
        public Vector3 HeadPosition { get; set; }

        /// <summary>
        /// The head's forward direction, in performer coordinates.
        /// </summary>
        public Vector3 HeadForwardDirection { get; set; }

        /// <summary>
        /// The position of the left hand, in performer coordinates.
        /// </summary>
        public Vector3 LeftHandPosition { get; set; }

        /// <summary>
        /// The position of the right hand, in performer coordinates.
        /// </summary>
        public Vector3 RightHandPosition { get; set; }

        /// <summary>
        /// The hand pose of the left hand.
        /// </summary>
        public HandPose LeftHandPose { get; set; }

        /// <summary>
        /// The hand pose of the right hand.
        /// </summary>
        public HandPose RightHandPose { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            HeadPosition = reader.GetVector3();
            HeadForwardDirection = reader.GetVector3();
            LeftHandPosition = reader.GetVector3();
            RightHandPosition = reader.GetVector3();
            LeftHandPose = HandPose.Deserialize(reader);
            RightHandPose = HandPose.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(HeadPosition);
            writer.Put(HeadForwardDirection);
            writer.Put(LeftHandPosition);
            writer.Put(RightHandPosition);
            HandPose.Serialize(writer, LeftHandPose);
            HandPose.Serialize(writer, RightHandPose);
        }
    }
}
