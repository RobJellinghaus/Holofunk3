// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Distributed
{
    /// <summary>
    /// Polls a NetManager.
    /// </summary>
    /// <remarks>
    /// Two vacuous subclasses of this handle polling before and after the rest of the Unity update cycle.
    /// 
    /// TODO: consider whether it would be better to do network polling on the FixedUpdate cycle.
    /// Not clear it would actually reduce latency at all assuming inter-frame packet delivery latency....
    /// </remarks>
    public abstract class Poller : MonoBehaviour
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
