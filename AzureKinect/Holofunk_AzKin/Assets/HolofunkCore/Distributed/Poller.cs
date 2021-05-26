/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Distributed
{
    /// <summary>
    /// Polls a NetManager.
    /// </summary>
    public class Poller : MonoBehaviour
    {
        public DistributedHoster distributedHoster;

        public Poller()
        {
        }

        /// <summary>
        /// Poll All The Things
        /// </summary>
        void Update()
        {
            if (distributedHoster != null)
            {
                // Poll the work queue both before and after the distributed host does its thing.
                distributedHoster.PollEvents();
            }
        }
    }
}
