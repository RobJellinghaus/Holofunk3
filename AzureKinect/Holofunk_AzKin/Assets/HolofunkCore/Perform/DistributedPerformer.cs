// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib;
using static Holofunk.Perform.PerformerMessages;

namespace Holofunk.Perform
{
    /// <summary>
    /// The distributed version of a performer in a Holofunk session.
    /// </summary>
    /// <remarks>
    /// Each HoloLens 2 host in a Holofunk system owns one DistributedPerformer.
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

        #region IDistributedPerformer

        public override ILocalObject LocalObject => GetLocalPerformer();

        private LocalPerformer GetLocalPerformer() => gameObject.GetComponent<LocalPerformer>();

        /// <summary>
        /// Get the performer.
        /// </summary>
        public PerformerState GetState() => GetLocalPerformer().GetState();

        [ReliableMethod]
        public void SetTouchedLoopies(DistributedId[] ids)
            => RouteReliableMessage(isRequest => new SetTouchedLoopies(Id, isRequest, ids));

        [ReliableMethod]
        public void AlterSoundEffect(EffectId effectId, int initialLevel, int alteration, bool commit)
            => RouteReliableMessage(isRequest => new AlterSoundEffect(Id, isRequest, effectId, initialLevel, alteration, commit));

        [ReliableMethod]
        public void PopSoundEffect() => RouteReliableMessage(isRequest => new PopSoundEffect(Id, isRequest));

        [ReliableMethod]
        public void ClearSoundEffects() => RouteReliableMessage(isRequest => new ClearSoundEffects(Id, isRequest));

        #endregion

        #region Standard meta-operations

        public override void OnDelete()
        {
            // propagate
            GetLocalPerformer().OnDelete();
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending PerformerMessages.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new Create(Id, GetState()), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't delete performers ever! they only disconnect, they never get deleted.
        }

        #endregion
    }
}
