/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Holofunk.Distributed;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Performer
{
    /// <summary>
    /// Serialized state of a performer (e.g. a person wearing a HoloLens 2).
    /// </summary>
    /// <remarks>
    /// If a joint is not currently tracked, all values for that joint will be zero.
    /// </remarks>
    public struct Performer : INetSerializable
    {
        /// <summary>
        /// The position of the head, as tracked by the performer's device, in performer coordinates.
        /// </summary>
        public Vector3 HeadPosition { get; set; }

        /// <summary>
        /// The position of the left hand, as tracked by the performer's device, in performer coordinates.
        /// </summary>
        public Vector3 LeftHandPosition { get; set; }

        /// <summary>
        /// The position of the right hand, as tracked by the performer's device, in performer coordinates.
        /// </summary>
        public Vector3 RightHandPosition { get; set; }

        /// <summary>
        /// The hand pose of the left hand, as tracked by the performer's device.
        /// </summary>
        public HandPose.HandPose LeftHandPose { get; set; }

        /// <summary>
        /// The hand pose of the right hand, as tracked by the performer's device.
        /// </summary>
        public HandPose.HandPose RightHandPose { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            HeadPosition = reader.GetVector3();
            LeftHandPosition = reader.GetVector3();
            RightHandPosition = reader.GetVector3();
            LeftHandPose = HandPose.HandPose.Deserialize(reader);
            RightHandPose = HandPose.HandPose.Deserialize(reader);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(HeadPosition);
            writer.Put(LeftHandPosition);
            writer.Put(RightHandPosition);
            HandPose.HandPose.Serialize(writer, LeftHandPose);
            HandPose.HandPose.Serialize(writer, RightHandPose);
        }
    }
}
