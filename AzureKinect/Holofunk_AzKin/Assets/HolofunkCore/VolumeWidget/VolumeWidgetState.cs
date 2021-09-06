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

namespace Holofunk.VolumeWidget
{
    /// <summary>
    /// Serialized state of a volume widget being used to adjust the volume of loopies.
    /// </summary>
    public struct VolumeWidgetState : INetSerializable
    {
        /// <summary>
        /// The position, in viewpoint coordinates.
        /// </summary>
        public Vector3 ViewpointPosition { get; set; }

        /// <summary>
        /// The current multiple, between 0.1 and 10.
        /// </summary>
        /// <remarks>
        /// This is the amount by which the widget has multiplied the volume since being created,
        /// and hence the amount that determines the widget's appearance.
        /// </remarks>
        public float VolumeRatio { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            ViewpointPosition = reader.GetVector3();
            VolumeRatio = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ViewpointPosition);
            writer.Put(VolumeRatio);
        }
    }
}
