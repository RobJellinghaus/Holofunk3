// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;
using static Holofunk.Viewpoint.SoundMessages;

namespace Holofunk.Sound
{
    public class DistributedSoundClock : DistributedComponent, IDistributedSoundClock
    {
        #region MonoBehaviours

        public void Start()
        {
            // If we have no ID yet, then we are an owning object that has not yet gotten an initial ID.
            // So, initialize ourselves as an owner.
            if (!Id.IsInitialized)
            {
                InitializeOwner();
            }
        }

        #endregion

        #region IDistributedSoundClock

        public override ILocalObject LocalObject => GetLocalSoundClock();

        public TimeInfo TimeInfo => GetLocalSoundClock().TimeInfo;

        private LocalSoundClock GetLocalSoundClock() => gameObject.GetComponent<LocalSoundClock>();

        #endregion

        #region Standard meta-operations

        #region Instantiation

        /// <summary>
        /// Create a new DistributedSoundClock with the given state.
        /// </summary>
        /// <remarks>
        /// This is how Performers learn what sound effects are available.
        /// 
        /// TODO: refactor Create methods to share more code.
        /// </remarks>
        public static GameObject Create(TimeInfo timeInfo)
        {
            GameObject prototypeEffect = DistributedObjectFactory.FindPrototypeContainer(
                DistributedObjectFactory.DistributedType.SoundEffect);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.SoundEffect);

            GameObject newEffect = Instantiate(prototypeEffect, localContainer.transform);
            // it will be inactive but that's actually good, it saves update cycles
            DistributedSoundClock distributedClock = newEffect.GetComponent<DistributedSoundClock>();
            LocalSoundClock localEffect = distributedClock.GetLocalSoundClock();

            // First set up the Loopie state in distributed terms.
            localEffect.Initialize(timeInfo);

            // Then enable the distributed behavior.
            distributedClock.InitializeOwner();

            // And finally set the loopie name.
            newEffect.name = $"{distributedClock.Id}";

            return newEffect;
        }

        #endregion
        public override void OnDelete()
        {
            // do nothing
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending SoundEffectMessages.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new CreateSoundClock(Id, TimeInfo), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't delete sound effects! never a reason to.
        }

        #endregion
    }
}
