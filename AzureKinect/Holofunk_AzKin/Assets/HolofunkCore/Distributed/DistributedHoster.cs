/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Distributed
{
    /// <summary>
    /// Singleton component which handles creating and initializing the DistributedHost for this app.
    /// </summary>
    public class DistributedHoster : MonoBehaviour
    {
        private IWorkQueue workQueue;
        private DistributedHost distributedHost;

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

        public DistributedHost DistributedHost => distributedHost;

        /// <summary>
        /// Create the singleton DistributedHost for this app.
        /// </summary>
        /// <remarks>
        /// This component should be in a GameObject at the very top of the scene, so this Start() method will run before all others.
        /// (Or at least before any PollComponents that use it.)
        /// </remarks>
        public void Start()
        {
            workQueue = new WorkQueue();
            distributedHost = new DistributedHost(workQueue, DefaultListenPort, isListener: true);
            distributedHost.RegisterType<PlayerId>();
            distributedHost.RegisterType(WriteVector3, ReadVector3);
        }

        /// <summary>
        /// This gets called by PollComponents to make polling happen at both the start and the end of the update cycle.
        /// </summary>
        public void PollEvents()
        {
            // Poll work queue both before and after distributed host does its thing.
            workQueue.PollEvents();
            distributedHost.PollEvents();
            workQueue.PollEvents();
        }

        public void OnDestroy()
        {
            distributedHost.Dispose();
        }

        private static void WriteVector3(NetDataWriter writer, Vector3 value)
        {
            writer.Put(value[0]);
            writer.Put(value[1]);
            writer.Put(value[2]);
        }

        private static Vector3 ReadVector3(NetDataReader reader)
        {
            return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }
    }
}
