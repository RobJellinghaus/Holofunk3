// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using static Holofunk.Loopie.LoopieMessages;

namespace Holofunk.Loopie
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
        public void SetMute(bool isMuted)
        {
            RouteReliableMessage(isRequest => new SetMute(Id, isRequest: !IsOwner, isMuted: isMuted));
        }

        #endregion

        #region Standard meta-operations

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
            HoloDebug.Log($"Sending PerformerMessages.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new Create(Id, GetLoopie()), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't do it!
        }

        #endregion
    }
}
