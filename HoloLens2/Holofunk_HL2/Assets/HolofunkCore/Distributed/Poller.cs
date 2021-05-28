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
        public Poller()
        {
        }

        /// <summary>
        /// Poll All The Things
        /// </summary>
        public void Update()
        {
            if (DistributedHoster.Instance != null)
            {
                // Poll the work queue both before and after the distributed host does its thing.
                DistributedHoster.Instance.PollEvents();
            }
        }

        /// <summary>
        /// Poll All The Things, Later
        /// </summary>
        public void LateUpdate()
        {
            if (DistributedHoster.Instance != null)
            {
                // Poll the work queue both before and after the distributed host does its thing.
                DistributedHoster.Instance.PollEvents();
            }
        }
    }
}
