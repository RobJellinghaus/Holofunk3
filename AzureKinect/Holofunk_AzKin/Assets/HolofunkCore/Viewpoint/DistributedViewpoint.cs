/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Holofunk.Viewpoint.ViewpointMessages;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// The distributed interface of a viewpoint in the current system.
    /// </summary>
    /// <remarks>
    /// Each Azure Kinect version of Holofunk connected to the current network will host its own DistributedViewpoint,
    /// which it uses to disseminate state about what it views.
    /// </remarks>
    public class DistributedViewpoint : DistributedComponent, IDistributedViewpoint
    {
        #region MonoBehaviours

        public virtual void Start()
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

        public override ILocalObject LocalObject => GetLocalViewpoint();

        private LocalViewpoint GetLocalViewpoint() => gameObject.GetComponent<LocalViewpoint>();

        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        public int PlayerCount => GetLocalViewpoint().PlayerCount;

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        public Player GetPlayer(int index) => GetLocalViewpoint().GetPlayer(index);

        /// <summary>
        /// Update the given player.
        /// </summary>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <remarks>
        /// There is no way to delete a player, but a player object can be marked untracked and have its fields
        /// nulled out.
        /// </remarks>
        [ReliableMethod]
        public void UpdatePlayer(Player playerToUpdate)
        {
            RouteReliableMessage(isRequest => new UpdatePlayer(Id, isRequest, playerToUpdate));
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

        public override void OnDetach()
        {
            // TODO: figure out what cleanup should happen here
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            Host.SendReliableMessage(new Create(Id, GetLocalViewpoint().PlayersAsArray), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't do it!
        }

        #endregion
    }
}
