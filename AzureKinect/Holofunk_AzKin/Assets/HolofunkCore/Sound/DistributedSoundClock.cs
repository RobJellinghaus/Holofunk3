// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;
using static Holofunk.Viewpoint.SoundMessages;

namespace Holofunk.Sound
{
    public class DistributedSoundClock : DistributedComponent, IDistributedSoundClock
    {
        /// <summary>
        /// The singleton sound clock we use for timing.
        /// </summary>
        public static DistributedSoundClock Instance { get; private set; }

        #region MonoBehaviours

        public void Start()
        {
            // If we have no ID yet, then we are an owning object that has not yet gotten an initial ID.
            // So, initialize ourselves as an owner.
            if (!Id.IsInitialized)
            {
                InitializeOwner();
            }

            if (gameObject.activeSelf && Instance == null)
            {
                Instance = this;
            }
        }

        #endregion

        #region IDistributedSoundClock

        public override ILocalObject LocalObject => GetLocalSoundClock();

        public TimeInfo TimeInfo => GetLocalSoundClock().TimeInfo;

        /// <summary>
        /// Beats per measure hardcoded to 4 for now. TODO: make this changeable
        /// </summary>
        public int BeatsPerMeasure => 4;

        public void UpdateTimeInfo(TimeInfo timeInfo)
            => RouteReliableMessage(isRequest => new UpdateSoundClockTimeInfo(Id, isRequest: isRequest, timeInfo: timeInfo));

        public void SetTempo(float beatsPerMinute, int beatsPerMeasure)
            => RouteReliableMessage(isRequest => new UpdateSoundClockTempo(Id, isRequest, beatsPerMinute, beatsPerMeasure));

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
                DistributedObjectFactory.DistributedType.SoundClock);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.SoundClock);

            GameObject newEffect = Instantiate(prototypeEffect, localContainer.transform);
            newEffect.SetActive(true);
            DistributedSoundClock distributedClock = newEffect.GetComponent<DistributedSoundClock>();
            LocalSoundClock localEffect = distributedClock.GetLocalSoundClock();

            // First, consume the initial state.
            localEffect.Initialize(timeInfo);

            // Then enable the distributed behavior.
            distributedClock.InitializeOwner();

            // Set the static instance.
            Instance = distributedClock;

            // And finally set the name.
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
