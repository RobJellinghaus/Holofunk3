// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Serialized state of a viewpoint.
    /// </summary>
    /// <remarks>
    /// This should probably really be per-audio-input state...
    /// </remarks>
    public struct ViewpointState : INetSerializable
    {
        /// <summary>
        /// Are we recording?
        /// </summary>
        public bool IsRecording { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            IsRecording = reader.GetBool();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(IsRecording);
        }
    }
}
