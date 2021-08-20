// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using static Holofunk.Viewpoint.SoundMessages;

namespace Holofunk.Sound
{
    public class DistributedSoundEffect : DistributedComponent, IDistributedSoundEffect
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

        #region IDistributedSoundEffect

        public override ILocalObject LocalObject => GetLocalSoundEffect();

        public int PluginId => GetLocalSoundEffect().PluginId;

        public string PluginName => GetLocalSoundEffect().PluginName;

        public int PluginProgramId => GetLocalSoundEffect().PluginProgramId;

        public string ProgramName => GetLocalSoundEffect().ProgramName;

        private LocalSoundEffect GetLocalSoundEffect() => gameObject.GetComponent<LocalSoundEffect>();

        #endregion

        #region Standard meta-operations

        public override void OnDelete()
        {
            // do nothing
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending SoundEffectMessages.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new Create(Id, PluginId, PluginName, PluginProgramId, ProgramName), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't delete sound effects! never a reason to.
        }

        #endregion
    }
}
