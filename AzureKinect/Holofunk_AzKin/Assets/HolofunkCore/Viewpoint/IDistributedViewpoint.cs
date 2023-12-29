// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
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
        [LocalMethod]
        int PlayerCount { get; }

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        [LocalMethod]
        PlayerState GetPlayerByIndex(int index);

        /// <summary>
        /// Try to get the player with this ID.
        /// </summary>
        /// <param name="playerId">The player ID to look for.</param>
        [LocalMethod]
        bool TryGetPlayerById(PlayerId playerId, out PlayerState player);

        /// <summary>
        /// Try to get the player that has been identified as being from the given host.
        /// </summary>
        bool TryGetPlayerByHostAddress(SerializedSocketAddress hostAddress, out PlayerState player);

        /// <summary>
        /// Update the given player.
        /// </summary>
        /// <param name="playerToUpdate">The player to update.</param>
        /// <remarks>
        /// There is no way to delete a player, but a player object can be marked untracked and have its fields
        /// nulled out.
        /// </remarks>
        [ReliableMethod]
        void UpdatePlayer(PlayerState playerToUpdate);

        /// <summary>
        /// Get the viewpoint-to-local matrix.
        /// </summary>
        /// <remarks>
        /// This will be Matrix4x4.zero if there is no connected viewpoint or if the current performer
        /// has not been recognized. This will be Matrix4x4.identity if this is the viewpoint host.
        /// </remarks>
        [LocalMethod]
        Matrix4x4 ViewpointToLocalMatrix();

        /// <summary>
        /// Get the local-to-viewpoint matrix.
        /// </summary>
        /// <remarks>
        /// This will be Matrix4x4.zero if there is no connected viewpoint or if the current performer
        /// has not been recognized. This will be Matrix4x4.identity if this is the viewpoint host.
        /// </remarks>
        [LocalMethod]
        Matrix4x4 LocalToViewpointMatrix();

        /// <summary>
        /// Are we recording?
        /// </summary>
        [LocalMethod]
        bool IsRecording { get; }

        /// <summary>
        /// Start recording.
        /// </summary>
        [ReliableMethod]
        void StartRecording();

        /// <summary>
        /// Stop recording.
        /// </summary>
        [ReliableMethod]
        void StopRecording();
    }
}
