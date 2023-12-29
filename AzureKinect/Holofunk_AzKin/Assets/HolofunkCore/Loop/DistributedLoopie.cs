// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib;
using UnityEngine;
using static Holofunk.Loop.LoopieMessages;

namespace Holofunk.Loop
{
    /// <summary>
    /// The distributed interface of a Loopie.
    /// </summary>
    /// <remarks>
    /// The initial model uses creator-owned Loopies. TBD whether we wind up wanting
    /// viewpoint-owned Loopies (for better experience if creators disconnect and reconnect).
    /// </remarks>
    public class DistributedLoopie : DistributedComponent, IDistributedLoopie
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

        #region IDistributedLoopie

        public override ILocalObject LocalObject => GetLocalLoopie();

        private LocalLoopie GetLocalLoopie() => gameObject == null ? null : gameObject.GetComponent<LocalLoopie>();

        /// <summary>
        /// Get the loopie state.
        /// </summary>
        public LoopieState GetLoopie() => GetLocalLoopie().GetLoopie();

        public void SetMute(bool isMuted) => 
            RouteReliableMessage(isRequest => new SetMute(Id, isRequest: !IsOwner, isMuted: isMuted));

        public void SetViewpointPosition(Vector3 viewpointPosition) =>
            RouteReliableMessage(isRequest => new SetViewpointPosition(Id, isRequest: !IsOwner, viewpointPosition: viewpointPosition));

        public void FinishRecording() =>
            RouteReliableMessage(isRequest => new FinishRecording(Id, isRequest: !IsOwner));

        public void AlterVolume(float alteration, bool commit) =>
            RouteReliableMessage(isRequest => new AlterVolume(Id, isRequest: !IsOwner, alteration, commit));

        public void AlterSoundEffect(EffectId effect, float alteration, bool commit) =>
            RouteReliableMessage(isRequest => new AlterSoundEffect(Id, !IsOwner, effect, alteration, commit));

        public void PopSoundEffect() =>
            RouteReliableMessage(isRequest => new PopSoundEffect(Id, isRequest: !IsOwner));

        public void ClearSoundEffects() =>
            RouteReliableMessage(isRequest => new ClearSoundEffects(Id, isRequest: !IsOwner));

        public void SetCurrentInfo(SignalInfo signalInfo, TrackInfo trackInfo, ulong timestamp) =>
            RouteBroadcastMessage(new SetCurrentInfo(Id, OwnerAddress, signalInfo, trackInfo, timestamp));

        public void SetCurrentWaveform(float[] frequencyBins, ulong timestamp) =>
            RouteBroadcastMessage(new SetCurrentWaveform(Id, OwnerAddress, frequencyBins, timestamp));

        #endregion

        #region DistributedState

        public override void OnDelete()
        {
            HoloDebug.Log($"DistributedLoopie.OnDelete: Deleting {Id}");
            // propagate locally
            GetLocalLoopie().OnDelete();
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"DistributedLoopie.SendCreateMessage: Sending Loopie.Create for id {Id} to peer {netPeer.EndPoint} with loopie {GetLoopie()}");
            Host.SendReliableMessage(new Create(Id, GetLoopie()), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            HoloDebug.Log($"DistributedLoopie.SendDeleteMessage: Sending Loopie.Delete for id {Id} to peer {netPeer.EndPoint} with loopie {GetLoopie()}");
            Host.SendReliableMessage(new Delete(Id, isRequest), netPeer);
        }

        #endregion

        #region Instantiation

        /// <summary>
        /// Create a new Loopie at this position in viewpoint space.
        /// </summary>
        /// <remarks>
        /// This is how Loopies come to exist on their owning hosts.
        /// 
        /// The loopie may be being recorded (if audioInputId is defined), or copied (if copiedLoopieId is defined).
        /// </remarks>
        public static GameObject Create(Vector3 viewpointPosition, NowSoundLib.AudioInputId audioInputId, DistributedId copiedLoopieId, int[] effects, int[] effectLevels)
        {
            HoloDebug.Assert(audioInputId == NowSoundLib.AudioInputId.AudioInputUndefined || copiedLoopieId == default(DistributedId),
                $"Exactly one of audioInputId {audioInputId} or copiedLoopieId {copiedLoopieId} must be defined -- loopies must be either recorded or copied");

            GameObject prototypeLoopie = DistributedObjectFactory.FindPrototypeContainer(
                DistributedObjectFactory.DistributedType.Loopie);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.Loopie);

            GameObject newLoopie = Instantiate(prototypeLoopie, localContainer.transform);
            newLoopie.SetActive(true);
            DistributedLoopie distributedLoopie = newLoopie.GetComponent<DistributedLoopie>();
            LocalLoopie localLoopie = distributedLoopie.GetLocalLoopie();

            localLoopie.Initialize(new LoopieState
            {
                AudioInput = new Sound.AudioInputId(audioInputId),
                CopiedLoopieId = copiedLoopieId,
                ViewpointPosition = viewpointPosition,
                IsMuted = false,
                Volume = 0.6f,
                Effects = (int[])effects.Clone(),
                EffectLevels = (int[])effectLevels.Clone()
            });

            // Then enable the distributed behavior.
            distributedLoopie.InitializeOwner();

            // And finally set the loopie name.
            newLoopie.name = $"{distributedLoopie.Id}";

            return newLoopie;
        }

        #endregion
    }
}
