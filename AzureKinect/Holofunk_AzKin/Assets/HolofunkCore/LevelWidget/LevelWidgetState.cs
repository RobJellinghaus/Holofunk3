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

namespace Holofunk.LevelWidget
{
    /// <summary>
    /// Serialized state of a level widget being used to adjust the strength of effects.
    /// </summary>
    public struct LevelWidgetState : INetSerializable
    {
        /// <summary>
        /// The position, in viewpoint coordinates.
        /// </summary>
        public Vector3 ViewpointPosition { get; set; }

        /// <summary>
        /// The current adjusted value applied by this widget, from -1 to 1 inclusive.
        /// </summary>
        /// <remarks>
        /// This is the amount by which the widget has multiplied the volume since being created,
        /// and hence the amount that determines the widget's appearance.
        /// </remarks>
        public float Adjustment { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            ViewpointPosition = reader.GetVector3();
            Adjustment = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(ViewpointPosition);
            writer.Put(Adjustment);
        }

        public override string ToString()
        {
            return $"LevelWidget[pos {ViewpointPosition}, adjustment {Adjustment}]";
        }
    }
}
