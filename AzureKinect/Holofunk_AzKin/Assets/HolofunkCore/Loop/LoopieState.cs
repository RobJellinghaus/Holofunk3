// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Sound;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// Serialized state of a loopie (e.g. a virtual recorded sound object).
    /// </summary>
    /// <remarks>
    /// Note that this does not contain any state about the actual sound. The actual sound is kept
    /// as local state by only the viewpoint's proxy instance of the loopie.
    /// </remarks>
    public struct LoopieState : INetSerializable
    {
        /// <summary>
        /// The audio input to start recording from.
        /// </summary>
        /// <remarks>
        /// This is only used during initial recording.
        /// 
        /// If the loopie is being copied, this will be Undefined.
        /// </remarks>
        public AudioInputId AudioInput { get; set; }

        /// <summary>
        /// The loopie from which this loopie is being copied, if any.
        /// </summary>
        /// <remarks>
        /// If the loopie is not being copied, this will be Undefined.
        /// </remarks>
        public DistributedId CopiedLoopieId { get; set; }

        /// <summary>
        /// The position of the loopie, in viewpoint coordinates.
        /// </summary>
        public Vector3 ViewpointPosition { get; set; }

        /// <summary>
        /// Is the loopie currently muted?
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// What is the current volume? (interval 0 to 1)
        /// </summary>
        public float Volume { get; set; }

        /// <summary>
        /// The list of sound effects applied to this Loopie, flattened into an int[].
        /// </summary>
        /// <remarks>
        /// Semantically this is an EffectId[] but we flatten it to a double-length int array,
        /// as NetDataReader in LiteNetLib doesn't seem to support the NetPacketProcessor-style type
        /// registration.
        /// 
        /// Clearing all sound effects is done by setting this to an empty array.
        /// </remarks>
        public int[] Effects { get; set; }

        /// <summary>
        /// The per-effect levels.
        /// </summary>
        /// <remarks>
        /// This is logically one-to-one with Effects, but since Effects has two ints per effect
        /// (plugin id, program id) and this has only one, Effects.Length == 2 * EffectLevels.Length.
        /// </remarks>
        public int[] EffectLevels { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            AudioInput = AudioInputId.Deserialize(reader);
            CopiedLoopieId = DistributedId.Deserialize(reader);
            ViewpointPosition = reader.GetVector3();
            IsMuted = reader.GetBool();
            Volume = reader.GetFloat();
            Effects = reader.GetIntArray();
            EffectLevels = reader.GetIntArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            AudioInputId.Serialize(writer, AudioInput);
            DistributedId.Serialize(writer, CopiedLoopieId);
            writer.Put(ViewpointPosition);
            writer.Put(IsMuted);
            writer.Put(Volume);
            writer.PutArray(Effects);
            writer.PutArray(EffectLevels);
        }

        public override string ToString() => $"Loopie[input {AudioInput}, copiedId {CopiedLoopieId} @ {ViewpointPosition}]";
    }
}
