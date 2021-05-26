/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Holofunk.Player
{
    public interface IDistributedPlayer : IDistributedInterface
    {
        /// <summary>
        /// Player identifier from Kinect (0 through N)
        /// </summary>
        public PlayerId PlayerId { get; }

        /// <summary>
        /// Is this player currently tracked?
        /// </summary>
        public bool Tracked { get; }

        /// <summary>
        /// If we know which Performer this is, this is the address of that Performer's Host.
        /// </summary>
        /// <remarks>
        /// This is the key means by which 
        /// </remarks>
        public SerializedSocketAddress HostAddress { get; }
    }
}
