// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Distributed;
using Holofunk.Hand;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Loopie
{
    /// <summary>
    /// Serialized state of a loopie (e.g. a virtual recorded sound object).
    /// </summary>
    /// <remarks>
    /// Note that this does not contain any state about the actual sound. The actual sound is kept
    /// as local state by only the viewpoint's proxy instance of the loopie.
    /// </remarks>
    public struct Loopie : INetSerializable
    {
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
            ViewpointPosition = reader.GetVector3();
            IsMuted = reader.GetBool();
            Volume = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ViewpointPosition);
            writer.Put(IsMuted);
            writer.Put(Volume);
        }
    }
}
