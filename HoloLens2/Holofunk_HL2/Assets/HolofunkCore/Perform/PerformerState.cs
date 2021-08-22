// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using Holofunk.Hand;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// Serialized state of a performer (e.g. a person wearing a HoloLens 2).
    /// </summary>
    /// <remarks>
    /// If a joint is not currently tracked, all values for that joint will be zero.
    /// </remarks>
    public struct PerformerState : INetSerializable
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

        /// <summary>
        /// The DistributedIds of the loopies this performer is currently touching.
        /// </summary>
        /// <remarks>
        /// Packet size limitations cause this to be bounded to a smallish number,
        /// such that this list doesn't outgrow a single packet.
        /// 
        /// LiteNetLib seems not to support arrays of serializable type, so this gets
        /// passed as a uint[] array instead of a DistributedId[] array.
        /// </remarks>
        public uint[] TouchedLoopieIdList { get; set; }

        /// <summary>
        /// Flattened list of (pluginId, pluginProgramId) int pairs, representing the sound effects
        /// currently applied to this Performer's audio.
        /// </summary>
        /// <remarks>
        /// Should get propagated to any Loopies created by this Performer.
        /// </remarks>
        public int[] Effects { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            HeadPosition = reader.GetVector3();
            HeadForwardDirection = reader.GetVector3();
            LeftHandPosition = reader.GetVector3();
            RightHandPosition = reader.GetVector3();
            LeftHandPose = HandPose.Deserialize(reader);
            RightHandPose = HandPose.Deserialize(reader);
            TouchedLoopieIdList = reader.GetUIntArray();
            Effects = reader.GetIntArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(HeadPosition);
            writer.Put(HeadForwardDirection);
            writer.Put(LeftHandPosition);
            writer.Put(RightHandPosition);
            HandPose.Serialize(writer, LeftHandPose);
            HandPose.Serialize(writer, RightHandPose);
            writer.PutArray(TouchedLoopieIdList);
            writer.PutArray(Effects);
        }

        /// <summary>
        /// Does this have the same Effects as the other state?
        /// </summary>
        public bool HasSameEffects(ref PerformerState other)
        {
            if (Effects == null && other.Effects == null)
            {
                return true;
            }
            if (Effects == null != (other.Effects == null))
            {
                return false;
            }
            if (Effects.Length != other.Effects.Length)
            {
                return false;
            }
            for (int i = 0; i < Effects.Length; i++)
            {
                if (Effects[i] != other.Effects[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
