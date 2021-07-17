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
    /// The distributed interface of a viewpoint.
    /// </summary>
    /// <remarks>
    /// Each Azure Kinect version of Holofunk in the current system will host its own DistributedViewpoint,
    /// which it uses to disseminate state about what it views.
    /// </remarks>
    public interface IDistributedViewpoint : IDistributedInterface
    {
        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        int PlayerCount { get; }

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        Player GetPlayer(int index);

        /// <summary>
        /// Try to get the player with this ID.
        /// </summary>
        /// <param name="playerId">The player ID to look for.</param>
        bool TryGetPlayer(PlayerId playerId, out Player player);

        /// <summary>
        /// Update the given player.
        /// </summary>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <remarks>
        /// There is no way to delete a player, but a player object can be marked untracked and have its fields
        /// nulled out.
        /// </remarks>
        void UpdatePlayer(Player playerToUpdate);
    }
}
