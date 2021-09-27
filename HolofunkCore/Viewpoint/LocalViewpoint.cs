// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using Holofunk.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// The local implementation of a Viewpoint object.
    /// </summary>
    /// <remarks>
    /// This keeps the local list of all players for this distributed viewpoint object on this host,
    /// whether this host is the owning host or not.
    /// </remarks>
    public class LocalViewpoint : MonoBehaviour, IDistributedViewpoint, ILocalObject
    {
        /// <summary>
        /// We keep the players list completely unsorted for now.
        /// </summary>
        private List<PlayerState> players = new List<PlayerState>();

        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        public int PlayerCount => players.Count;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedViewpoint>();

        internal void Initialize(PlayerState[] playerArray) => players.AddRange(playerArray);

        /// <summary>
        /// Internal method for use by Create message.
        /// </summary>
        internal PlayerState[] PlayersAsArray => players.ToArray();

        /// <summary>
        /// The state of this viewpoint.
        /// </summary>
        private ViewpointState state;

        #region IDistributedViewpoint

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        /// <param name="index">Zero-based index of player to retrieve.</param>
        /// <remarks>
        /// Note that the index of the player here has nothing to do with the PlayerId field of the player;
        /// this index is semantically meaningless and only used for iterating over currently known players.
        /// </remarks>
        public PlayerState GetPlayer(int index)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < PlayerCount);

            return players[index];
        }

        /// <summary>
        /// Try to get the player with a given Id.
        /// </summary>
        public bool TryGetPlayer(PlayerId playerId, out PlayerState player)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    player = players[i];
                    return true;
                }
            }
            player = default(PlayerState);
            return false;
        }

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
        public void UpdatePlayer(PlayerState playerToUpdate)
        {
            //Core.HoloDebug.Log($"UpdatePlayer: updating player {playerToUpdate.PlayerId} with performer host address {playerToUpdate.PerformerHostAddress}");
            for (int i = 0; i < PlayerCount; i++)
            {
                if (playerToUpdate.PlayerId == players[i].PlayerId)
                {
                    players[i] = playerToUpdate;
                    return;
                }
            }
            players.Add(playerToUpdate);
        }

        /// <summary>
        /// The Player which has been recognized as being from this host.
        /// </summary>
        /// <remarks>
        /// default(Player) if this is the viewpoint host (corresponding to no player).
        /// default(Player) if the player corresponding to this performer has not been recognized.
        /// 
        /// This is used for acquiring the viewpoint-to-local matrices, since these are not defined
        /// unless the current performer is recognized.
        /// </remarks>
        private PlayerState GetLocalPlayer()
        {
            // If we are the viewpoint host then there is no local player.
            if (DistributedObject.IsOwner)
            {
                return default(PlayerState);
            }

            // what is our current host?
            SerializedSocketAddress hostAddress = DistributedObject.Host.SocketAddress;
            TryGetPlayer(hostAddress, out PlayerState p);
            return p;
        }

        /// <summary>
        /// Try to get the player associated with the performer from the given host.
        /// </summary>
        public bool TryGetPlayer(SerializedSocketAddress hostAddress, out PlayerState player)
        {
            // do we have a player that has been recognized as being from here?
            for (int i = 0; i < PlayerCount; i++)
            {
                PlayerState p = GetPlayer(i);
                if (p.PerformerHostAddress == hostAddress)
                {
                    // found it!
                    player = p;
                    return true;
                }
            }

            // didn't find it
            player = default(PlayerState);
            return false;
        }

        public Matrix4x4 ViewpointToLocalMatrix()
        {
            if (DistributedObject.IsOwner)
            {
                return Matrix4x4.identity;
            }
            else
            {
                PlayerState localPlayer = GetLocalPlayer();
                if (localPlayer.PlayerId.IsInitialized)
                {
                    return localPlayer.ViewpointToPerformerMatrix;
                }
                else
                {
                    return Matrix4x4.zero;
                }
            }
        }

        public Matrix4x4 LocalToViewpointMatrix()
        {
            if (DistributedObject.IsOwner)
            {
                return Matrix4x4.identity;
            }
            else
            {
                PlayerState localPlayer = GetLocalPlayer();
                if (localPlayer.PlayerId.IsInitialized)
                {
                    return localPlayer.PerformerToViewpointMatrix;
                }
                else
                {
                    return Matrix4x4.zero;
                }
            }
        }

        public bool IsRecording => state.IsRecording;

        public void StartRecording()
        {
            state.IsRecording = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StartRecording();
            }
        }

        public void StopRecording()
        {
            state.IsRecording = false;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopRecording();
            }
        }

        #endregion
    }
}
