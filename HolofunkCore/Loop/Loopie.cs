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
    public struct Loopie : INetSerializable
    {
        /// <summary>
        /// The audio input to start recording from.
        /// </summary>
        public AudioInput AudioInput { get; set; }

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

        public void Deserialize(NetDataReader reader)
        {
            AudioInput = AudioInput.Deserialize(reader);
            ViewpointPosition = reader.GetVector3();
            IsMuted = reader.GetBool();
            Volume = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            AudioInput.Serialize(writer, AudioInput);
            writer.Put(ViewpointPosition);
            writer.Put(IsMuted);
            writer.Put(Volume);
        }

        public override string ToString() => $"Loopie[{AudioInput}, @{ViewpointPosition}]";
    }
}
