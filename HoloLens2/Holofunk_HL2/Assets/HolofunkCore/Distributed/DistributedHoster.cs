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

        public static DistributedHoster Instance { get; private set; }

        public static DistributedHost Host { get; private set; }

        private IWorkQueue workQueue;

        public DistributedHoster()
        {
        }

        /// <summary>
        /// Create the singleton DistributedHost for this app.
        /// </summary>
        /// <remarks>
        /// This should be the only Awake() method in Holofunk, at least if we want to rely on this for ensuring the
        /// DistributedHost is created first. Otherwise we may need to use some other mechanism, or use on-demand
        /// Host initialization, or something.
        /// </remarks>
        public void Awake()
        {
            workQueue = new WorkQueue();
            Host = new DistributedHost(workQueue, DefaultListenPort, isListener: true);
            Host.RegisterType<PlayerId>();
            Host.RegisterType(WriteVector3, ReadVector3);
            Instance = this;
        }

        /// <summary>
        /// This gets called by PollComponents to make polling happen at both the start and the end of the update cycle.
        /// </summary>
        public void PollEvents()
        {
            // Poll work queue both before and after distributed host does its thing.
            workQueue.PollEvents();
            Host.PollEvents();
            workQueue.PollEvents();
        }

        public void OnDestroy()
        {
            Host.Dispose();
            Host = null;
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
