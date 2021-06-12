﻿/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using static Holofunk.Performer.PerformerMessages;

namespace Holofunk.Performer
{
    /// <summary>
    /// The distributed interface of a viewpoint in the current system.
    /// </summary>
    /// <remarks>
    /// Each HoloLens 2 host in a Holofunk system 
    /// </remarks>
    public class DistributedPerformer : DistributedComponent, IDistributedPerformer
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

        #region IDistributedViewpoint

        public override ILocalObject LocalObject => GetLocalPerformer();

        private LocalPerformer GetLocalPerformer() => gameObject.GetComponent<LocalPerformer>();

        /// <summary>
        /// Get the performer.
        /// </summary>
        public Performer GetPerformer() => GetLocalPerformer().GetPerformer();

        /// <summary>
        /// Update the given player.
        /// </summary>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <remarks>
        /// There is no way to delete a player, but a player object can be marked untracked and have its fields
        /// nulled out.
        /// </remarks>
        [ReliableMethod]
        public void UpdatePerformer(Performer performer)
        {
            RouteReliableMessage(isRequest => new UpdatePerformer(Id, isRequest, performer));
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
            Host.SendReliableMessage(new Create(Id, GetPerformer()), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't do it!
        }

        #endregion
    }
}