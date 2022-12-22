// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Sound;
using Holofunk.Viewpoint;
using NowSoundLib;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// The local implementation of a performer.
    /// </summary>
    public class LocalPerformer : MonoBehaviour, IDistributedPerformer, ILocalObject
    {
        /// <summary>
        /// We keep the players list completely unsorted for now.
        /// </summary>
        private PerformerState performer;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedPerformer>();

        internal void Initialize(PerformerState performer)
        {
            this.performer = performer;
        }

        /// <summary>
        /// Get the (singular) performer.
        /// </summary>
        public PerformerState GetPerformer() => performer;

        public void OnDelete()
        {
            // TODO: Delete the performer's effects on the audio input
        }

        /// <summary>
        /// Update the performer.
        /// </summary>
        public void UpdatePerformer(PerformerState performer)
        {
            // did the list of sound effects on the performer change?
            // TODO: switch to using method style as with Loopie, since it avoids needing to support arbitrary changes.
            if (!this.performer.HasSameEffects(ref performer))
            {
                // effects changed.
                // for now we support only either clearing them all or appending one at a time
                if (performer.Effects == null)
                {
                    performer.Effects = new int[0];
                }
                if (this.performer.Effects == null)
                {
                    this.performer.Effects = new int[0];
                }
                if (performer.Effects.Length == 0)
                {
                    HoloDebug.Log("LocalPerformer.UpdatePerformer: effects cleared, calling ClearPerformerEffects");
                    ClearPerformerEffects();
                }
                else if (performer.Effects.Length == this.performer.Effects.Length + 2)
                {
                    // assume appending
                    HoloDebug.Log("LocalPerformer.UpdatePerformer: effects got longer by 2, assuming append");
                    AppendPerformerEffect(
                        new EffectId(
                            new Sound.PluginId((NowSoundLib.PluginId)performer.Effects[performer.Effects.Length - 2]),
                            new PluginProgramId((ProgramId)performer.Effects[performer.Effects.Length - 1])));
                }
                else
                {
                    HoloDebug.Log($"LocalPerformer.UpdatePerformer: effects went from {this.performer.Effects.Length} to {performer.Effects.Length} -- making no fx changes");
                }
            }

            this.performer = performer;
        }

        private void AppendPerformerEffect(EffectId effect)
        {
            if (SoundManager.Instance != null)
            {
                // this performer is a proxy, we know this. so, where did it come from?
                // we assigned it a player already and from that player we can get their assigned audio input
                // (which we, the viewpoint, are in charge of assigning).
                // TODO: add audio input ID assignment to player state
                bool foundPlayer = DistributedViewpoint.Instance.TryGetPlayerByHostAddress(
                    DistributedObject.OwnerAddress,
                    out PlayerState playerState);

                if (foundPlayer)
                {
                    NowSoundLib.AudioInputId playerAudioInput = NowSoundLib.AudioInputId.AudioInput1;

                    int playerEffectCount = NowSoundGraphAPI.GetInputPluginInstanceCount(playerAudioInput);

                    HoloDebug.Log($"LocalPerformer.AppendPerformerEffect: appending [{effect.PluginId}, {effect.PluginProgramId}] with {playerEffectCount} effects already active");

                    NowSoundGraphAPI.AddInputPluginInstance(
                        NowSoundLib.AudioInputId.AudioInput1,
                        effect.PluginId.Value,
                        effect.PluginProgramId.Value,
                        100);
                }
                else
                {
                    HoloDebug.Log("LocalPerformer.AppendPerformerEffect: Did not find player");
                }
            }
        }

        private void ClearPerformerEffects()
        {
            if (SoundManager.Instance != null)
            {
                // this performer is a proxy, we know this. so, where did it come from?
                // we assigned it a player already and from that player we can get their assigned audio input
                // (which we, the viewpoint, are in charge of assigning).
                // TODO: add audio input ID assignment to player state
                bool foundPlayer = DistributedViewpoint.Instance.TryGetPlayerByHostAddress(
                    DistributedObject.OwnerAddress,
                    out PlayerState playerState);

                if (foundPlayer)
                {
                    NowSoundLib.AudioInputId playerAudioInput = NowSoundLib.AudioInputId.AudioInput1;

                    int playerEffectCount = NowSoundGraphAPI.GetInputPluginInstanceCount(playerAudioInput);
                    HoloDebug.Log($"LocalPerformer.ClearPerformerEffects: clearing {playerEffectCount} effects");

                    for (int i = 0; i < playerEffectCount; i++)
                    {
                        // Each time we remove one the indices of the later ones change.
                        // So just remove index 1 over and over.
                        // TODO: really use stable IDs if it makes sense to do so later (e.g. direct instance manipulation from the app).
                        // TODO: ...or just add a plugin instance query API to NowSoundLib and ask NowSoundLib what's the deal.
                        NowSoundGraphAPI.DeleteInputPluginInstance(playerAudioInput, (PluginInstanceIndex)1);
                    }
                }
                else
                {
                    HoloDebug.Log("LocalPerformer.ClearPerformerEffects: Did not find player");
                }
            }
        }
    }
}
