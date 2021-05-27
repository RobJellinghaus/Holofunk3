/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// The distributed interface of a viewpoint in the current system.
    /// </summary>
    /// <remarks>
    /// Each Azure Kinect version of Holofunk connected to the current network will host its own DistributedViewpoint,
    /// which it uses to disseminate state about what it views.
    /// </remarks>
    public class DistributedViewpoint : MonoBehaviour, IDistributedViewpoint
    {
        private LocalViewpoint GetLocalObject() => gameObject.GetComponent<LocalViewpoint>();

        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        public int PlayerCount => GetLocalObject().PlayerCount;

        public DistributedId Id => throw new NotImplementedException();

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        public Player GetPlayer(int index) => GetLocalObject().GetPlayer(index);

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

        }

        public void OnDelete()
        {
            throw new NotImplementedException();
        }
    }
}
