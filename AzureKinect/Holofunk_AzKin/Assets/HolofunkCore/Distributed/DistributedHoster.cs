/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib;
using UnityEngine;

namespace Holofunk.Distributed
{
    /// <summary>
    /// Singleton component which handles creating and initializing the DistributedHost for this app.
    /// </summary>
    public class DistributedHoster : MonoBehaviour
    {
        private IWorkQueue _workQueue;
        private DistributedHost _distributedHost;

        /// <summary>
        /// Random port that happened to be, not only open, but with no other UDP or TCP ports in the 9??? range
        /// on my local Windows laptop.
        /// </summary>
        public static ushort DefaultListenPort = 30303;

        /// <summary>
        /// Random port that happened to be, not only open, but with no other UDP or TCP ports in the 3????? range
        /// on my local Windows laptop.
        /// </summary>
        public static ushort DefaultReliablePort = 30304;

        public DistributedHoster()
        {
        }

        /// <summary>
        /// Create the singleton DistributedHost for this app.
        /// </summary>
        /// <remarks>
        /// This component should be in a GameObject at the very top of the scene, so this Start() method will run before all others.
        /// (Or at least before any PollComponents that use it.)
        /// </remarks>
        public void Start()
        {
            _workQueue = new WorkQueue();
            _distributedHost = new DistributedHost(_workQueue, DefaultListenPort, isListener: true);
        }

        /// <summary>
        /// This gets called by PollComponents to make polling happen at both the start and the end of the update cycle.
        /// </summary>
        public void PollEvents()
        {
            // Poll work queue both before and after distributed host does its thing.
            _workQueue.PollEvents();
            _distributedHost.PollEvents();
            _workQueue.PollEvents();
        }

        public void OnDestroy()
        {
            _distributedHost.Dispose();
        }
    }
}
