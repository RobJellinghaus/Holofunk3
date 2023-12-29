﻿// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;

namespace Holofunk.LevelWidget
{
    /// <summary>
    /// The distributed interface of a LevelWidget.
    /// </summary>
    public interface IDistributedLevelWidget : IDistributedInterface
    {
        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        [LocalMethod]
        LevelWidgetState State { get; }

        /// <summary>
        /// Update the state.
        /// </summary>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <remarks>
        /// There is no way to delete a player, but a player object can be marked untracked and have its fields
        /// nulled out.
        /// </remarks>
        [ReliableMethod]
        void UpdateState(LevelWidgetState state);
    }
}
