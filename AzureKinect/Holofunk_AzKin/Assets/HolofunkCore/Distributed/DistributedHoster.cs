// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Loop;
using Holofunk.Menu;
using Holofunk.Perform;
using Holofunk.Sound;
using Holofunk.Viewpoint;
using Holofunk.LevelWidget;
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

        /// <summary>
        /// is this application hosting audio?
        /// </summary>
        public bool isHostingAudio = false;

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
            workQueue = new WorkQueue(HoloDebug.Instance);
            Host = new DistributedHost(workQueue, DefaultListenPort, isListener: true, logger: HoloDebug.Instance);
            
            // Unity types
            Host.RegisterType(SerializationExtensions.Put, SerializationExtensions.GetVector3);
            Host.RegisterType(SerializationExtensions.Put, SerializationExtensions.GetVector4);
            Host.RegisterType(SerializationExtensions.Put, SerializationExtensions.GetMatrix4x4);

            // Distributed data types
            Host.RegisterWith(LoopieMessages.RegisterTypes);
            Host.RegisterWith(MenuMessages.RegisterTypes);
            Host.RegisterWith(PerformerMessages.RegisterTypes);
            Host.RegisterWith(SoundMessages.RegisterTypes);
            Host.RegisterWith(ViewpointMessages.RegisterTypes);
            Host.RegisterWith(LevelWidgetMessages.RegisterTypes);

            // Distributed message types; must be registered only after all types have been registered
            Host.RegisterWith(LoopieMessages.Register);
            Host.RegisterWith(MenuMessages.Register);
            Host.RegisterWith(PerformerMessages.Register);
            Host.RegisterWith(SoundMessages.Register);
            Host.RegisterWith(ViewpointMessages.Register);
            Host.RegisterWith(LevelWidgetMessages.Register);

            Instance = this;

            // let the announcements begin!
            Host.Announce(isHostingAudio);
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
    }
}
