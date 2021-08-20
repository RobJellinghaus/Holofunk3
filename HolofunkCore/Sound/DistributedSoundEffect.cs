// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using UnityEngine;
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

        public PluginId PluginId => GetLocalSoundEffect().PluginId;

        public string PluginName => GetLocalSoundEffect().PluginName;

        public PluginProgramId PluginProgramId => GetLocalSoundEffect().PluginProgramId;

        public string ProgramName => GetLocalSoundEffect().ProgramName;

        private LocalSoundEffect GetLocalSoundEffect() => gameObject.GetComponent<LocalSoundEffect>();

        #endregion

        #region Standard meta-operations

        #region Instantiation

        /// <summary>
        /// Create a new DistributedSoundEffect with the given state.
        /// </summary>
        /// <remarks>
        /// This is how Performers learn what sound effects are available.
        /// 
        /// TODO: refactor Create methods to share more code.
        /// </remarks>
        public static GameObject Create(PluginId pluginId, string pluginName, PluginProgramId pluginProgramId, string programName)
        {
            GameObject prototypeEffect = DistributedObjectFactory.FindPrototypeContainer(
                DistributedObjectFactory.DistributedType.SoundEffect);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.SoundEffect);

            GameObject newEffect = Instantiate(prototypeEffect, localContainer.transform);
            // it will be inactive but that's actually good, it saves update cycles
            DistributedSoundEffect distributedEffect = newEffect.GetComponent<DistributedSoundEffect>();
            LocalSoundEffect localEffect = distributedEffect.GetLocalSoundEffect();

            // First set up the Loopie state in distributed terms.
            localEffect.Initialize(pluginId, pluginName, pluginProgramId, programName);

            // Then enable the distributed behavior.
            distributedEffect.InitializeOwner();

            // And finally set the loopie name.
            newEffect.name = $"{distributedEffect.Id}";

            return newEffect;
        }

        #endregion
        public override void OnDelete()
        {
            // do nothing
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"Sending SoundEffectMessages.Create for id {Id} to peer {netPeer.EndPoint}");
            Host.SendReliableMessage(new CreateSoundEffect(Id, PluginId, PluginName, PluginProgramId, ProgramName), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            // don't delete sound effects! never a reason to.
        }

        #endregion
    }
}
