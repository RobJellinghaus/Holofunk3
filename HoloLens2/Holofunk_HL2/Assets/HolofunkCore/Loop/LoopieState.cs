// Copyright by Rob Jellinghaus. All rights reserved.

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
    /// 
    /// Also note that the amplitude (current loudness) of the loopie is broadcast "ephemerally"
    /// from one proxy to all instances, and isn't part of the "persistent" distributed state of the loopie.
    /// </remarks>
    public struct LoopieState : INetSerializable
    {
        /// <summary>
        /// The audio input to start recording from.
        /// </summary>
        public AudioInputId AudioInput { get; set; }

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

        public void Deserialize(NetDataReader reader)
        {
            AudioInput = AudioInputId.Deserialize(reader);
            ViewpointPosition = reader.GetVector3();
            IsMuted = reader.GetBool();
            Volume = reader.GetFloat();
            Effects = reader.GetIntArray();
        }

        public void Serialize(NetDataWriter writer)
        {
            AudioInputId.Serialize(writer, AudioInput);
            writer.Put(ViewpointPosition);
            writer.Put(IsMuted);
            writer.Put(Volume);
            writer.PutArray(Effects);
        }

        public override string ToString() => $"Loopie[{AudioInput}, @{ViewpointPosition}]";
    }
}
