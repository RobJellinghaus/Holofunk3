// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
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

        public override ILocalObject LocalObject => GetLocalViewpoint();

        private LocalViewpoint GetLocalViewpoint() => gameObject.GetComponent<LocalViewpoint>();

        /// <summary>
        /// Get the count of currently known Players.
        /// </summary>
        public int PlayerCount => GetLocalViewpoint().PlayerCount;

        /// <summary>
        /// Get the player with a given index.
        /// </summary>
        public Player GetPlayer(int index) => GetLocalViewpoint().GetPlayer(index);

        /// <summary>
        /// Try to get the player with this ID.
        /// </summary>
        public bool TryGetPlayer(PlayerId playerId, out Player player) => GetLocalViewpoint().TryGetPlayer(playerId, out player);

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

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending ViewpointMessages.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new Create(Id, GetLocalViewpoint().PlayersAsArray), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't do it!
        }

        #endregion
    }
}
