/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Serialized state of a player (e.g. a recognized/tracked individual) as seen from a viewpoint.
    /// </summary>
    public struct Player
    {
        /// <summary>
        /// Player identifier from the Viewpoint (0 through N)
        /// </summary>
        public PlayerId PlayerId { get; set; }

        /// <summary>
        /// Is this player currently tracked?
        /// </summary>
        public bool Tracked { get; set; }

        /// <summary>
        /// If we know which Performer this is, this is the address of that Performer's Host.
        /// </summary>
        /// <remarks>
        /// This is the key means by which the viewpoint arbitrates which other coordinate space
        /// it believes this player is hosted in.
        /// </remarks>
        public SerializedSocketAddress PerformerHostAddress { get; set; }

        /// <summary>
        /// The ID of the performer on that host that we think is this player.
        /// </summary>
        public PerformerId PerformerId { get; set; }

        /// <summary>
        /// The position of the head, as seen from the viewpoint, in viewpoint coordinates.
        /// </summary>
        public Vector3 HeadPosition { get; set; }

        /// <summary>
        /// The position of the left hand, as seen from the viewpoint, in viewpoint coordinates.
        /// </summary>
        public Vector3 LeftHandPosition { get; set; }

        /// <summary>
        /// The position of the right hand, as seen from the viewpoint, in viewpoint coordinates.
        /// </summary>
        public Vector3 RightHandPosition { get; set; }
    }
}
