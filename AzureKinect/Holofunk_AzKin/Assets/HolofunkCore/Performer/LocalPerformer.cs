/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Performer
{
    /// <summary>
    /// The local implementation of a Viewpoint object.
    /// </summary>
    /// <remarks>
    /// This keeps the local list of all players for this distributed viewpoint object on this host,
    /// whether this host is the owning host or not.
    /// </remarks>
    public class LocalPerformer : MonoBehaviour, IDistributedPerformer, ILocalObject
    {
        /// <summary>
        /// We keep the players list completely unsorted for now.
        /// </summary>
        private Performer performer;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedPerformer>();

        internal void Initialize(Performer performer)
        {
            this.performer = performer;
        }

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        public Performer GetPerformer() => performer;

        public void OnDelete()
        {
            // Go gently
        }

        /// <summary>
        /// Update the given player.
        /// </summary>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <remarks>
        /// There is no way to delete a player, but a player object can be marked untracked and have its fields
        /// nulled out (except for player ID).
        /// </remarks>
        public void UpdatePerformer(Performer performer)
        {
            this.performer = performer;
        }
    }
}
