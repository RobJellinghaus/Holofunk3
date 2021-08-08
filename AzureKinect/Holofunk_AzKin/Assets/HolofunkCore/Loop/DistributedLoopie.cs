// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
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

        private LocalLoopie GetLocalLoopie() => gameObject.GetComponent<LocalLoopie>();

        /// <summary>
        /// Get the loopie state.
        /// </summary>
        public Loopie GetLoopie() => GetLocalLoopie().GetLoopie();

        /// <summary>
        /// Set whether the loopie is muted.
        /// </summary>
        [ReliableMethod]
        public void SetMute(bool isMuted) => 
            RouteReliableMessage(isRequest => new SetMute(Id, isRequest: !IsOwner, isMuted: isMuted));

        /// <summary>
        /// Set whether the loopie is muted.
        /// </summary>
        [ReliableMethod]
        public void SetVolume(float volume) =>
            RouteReliableMessage(isRequest => new SetVolume(Id, isRequest: !IsOwner, volume: volume));


        public void SetViewpointPosition(Vector3 viewpointPosition) =>
            RouteReliableMessage(isRequest => new SetViewpointPosition(Id, isRequest: !IsOwner, viewpointPosition: viewpointPosition));

        public void FinishRecording() =>
            RouteReliableMessage(isRequest => new FinishRecording(Id, isRequest: !IsOwner));

        public void SetCurrentAmplitude(float min, float avg, float max) =>
            RouteBroadcastMessage(new SetCurrentAmplitude(Id, new SerializedSocketAddress(OwningPeer), min, avg, max));

        #endregion

        #region DistributedState

        public override void Delete()
        {
            // No-op; Viewpoints are never deleted, they just leave the system
        }

        public override void OnDelete()
        {
            // No-op; Viewpoints are never deleted, only detached
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending Loopie.Create for id {Id} to peer {netPeer.EndPoint} with loopie {GetLoopie()}");
            Host.SendReliableMessage(new Create(Id, GetLoopie()), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't do it!
        }

        #endregion

        #region Instantiation

        /// <summary>
        /// Create a new Loopie at this position in viewpoint space.
        /// </summary>
        /// <remarks>
        /// This is how Loopies come to exist on their owning hosts.
        /// 
        /// TODO: figure out how to refactor out the shared plumbing here, similarly to the Registrar.
        /// </remarks>
        public static GameObject Create(Vector3 viewpointPosition)
        {
            GameObject prototypeLoopie = DistributedObjectFactory.FindPrototype(
                DistributedObjectFactory.DistributedType.Loopie);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.Loopie);

            GameObject newLoopie = Instantiate(prototypeLoopie, localContainer.transform);
            newLoopie.SetActive(true);
            DistributedLoopie distributedLoopie = newLoopie.GetComponent<DistributedLoopie>();
            LocalLoopie localLoopie = distributedLoopie.GetLocalLoopie();

            // First set up the Loopie state in distributed terms.
            localLoopie.Initialize(new Loopie
            {
                AudioInput = new Sound.AudioInput(NowSoundLib.AudioInputId.AudioInput1),
                ViewpointPosition = viewpointPosition,
                IsMuted = false,
                Volume = 0.7f
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
