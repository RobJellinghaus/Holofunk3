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
        /// State of this performer.
        /// </summary>
        private PerformerState state;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedPerformer>();

        /// <summary>
        /// If this is a non-distributed LocalPerformer that is just statically part of a Player,
        /// then initialize its state at this point.
        /// </summary>
        public void Start()
        {
            if (this.state.Effects == null)
            {
                this.state = new PerformerState
                {
                    Effects = new int[0],
                    EffectLevels = new int[0],
                    TouchedLoopieIdList = new uint[0],
                };
            }
        }

        internal void Initialize(PerformerState state)
        {
            this.state = state;

            // TODO: make this set up initial effects properly.
            // For now we assume that Performers never have effects to begin with, and that
            // Performer proxies never exist.
        }

        /// <summary>
        /// Get the (singular) performer.
        /// </summary>
        public PerformerState GetState() => state;

        public void OnDelete()
        {
            // TODO: Delete the performer's effects on the audio input
        }

        /// <summary>
        /// Update the performer.
        /// </summary>
        public void SetTouchedLoopies(DistributedId[] loopieIds)
        {
            uint[] ids = new uint[loopieIds.Length];
            for (int i = 0; i < loopieIds.Length; i++)
            {
                ids[i] = loopieIds[i].Value;
            }
            this.state.TouchedLoopieIdList = ids;
        }

        public void AlterVolume(float alteration, bool commit)
        {
            // no-op for performers. learn to sing quieter
        }

        public void AlterSoundEffect(EffectId effect, float alteration, bool commit)
        {
            PerformerState state = GetState();
            // TODO: use the PlayerPerformerMapper again. But for now, we're in the same damn object.
            int playerIndex = GetComponent<PlayerUpdater>().PlayerIndex;

            int effectIndex = effect.FindIn(state.Effects);
            if (effectIndex == -1)
            {
                HoloDebug.Log($"LocalPerformer.AlterSoundEffect: did not find effect ID {effect} in effect list {state.Effects.ArrayToString()}, creating new plugin");

                state.Effects = effect.AppendTo(state.Effects);
                // TODO: make per-effect custom initial levels here (they don't need to be passed in the verb/message)
                int initialLevel = 100;
                state.EffectLevels = EffectId.AppendTo(state.EffectLevels, initialLevel);
                effectIndex = state.EffectLevels.Length - 1;

                if (SoundManager.Instance != null)
                {
                    NowSoundGraphAPI.AddInputPluginInstance(
                        (NowSoundLib.AudioInputId.AudioInput1 + playerIndex),
                        effect.PluginId.Value,
                        effect.PluginProgramId.Value,
                        initialLevel);
                }
            }

            HoloDebug.Log($"LocalPerformer.AlterSoundEffect: about to alter effect index {effectIndex} (Effects {state.Effects.ArrayToString()}, EffectLevels {state.EffectLevels.ArrayToString()}");
            int newLevel = state.EffectLevels[effectIndex] + (int)(alteration * MagicNumbers.EffectLevelScale);
            newLevel = Mathf.Clamp(newLevel, 0, 100);

            if (SoundManager.Instance != null)
            {
                NowSoundGraphAPI.SetInputPluginInstanceDryWet(
                    (NowSoundLib.AudioInputId.AudioInput1 + playerIndex),
                    (PluginInstanceIndex)(effectIndex + 1),
                    newLevel);
            }

            if (commit)
            {
                state.EffectLevels[effectIndex] = newLevel;
            }

            // and update the state
            this.state = state;
        }

        public void PopSoundEffect()
        {
            PerformerState state = GetState();
            if (state.EffectLevels.Length == 0)
            {
                return; // nothing to pop
            }

            // TODO: use the PlayerPerformerMapper again. But for now, we're in the same damn object.
            int playerIndex = GetComponent<PlayerUpdater>().PlayerIndex;
            NowSoundLib.AudioInputId playerAudioInput = NowSoundLib.AudioInputId.AudioInput1 + playerIndex;

            state.Effects = EffectId.PopFrom(state.Effects, 2);
            state.EffectLevels = EffectId.PopFrom(state.EffectLevels, 1);

            if (SoundManager.Instance != null)
            {
                NowSoundGraphAPI.DeleteInputPluginInstance(playerAudioInput, (PluginInstanceIndex)(state.EffectLevels.Length + 1));
            }

            // and update the state!!!
            this.state = state;
        }

        public void ClearSoundEffects()
        {
            PerformerState state = GetState();
            // TODO: use the PlayerPerformerMapper again. But for now, we're in the same damn object.
            int playerIndex = GetComponent<PlayerUpdater>().PlayerIndex;
            NowSoundLib.AudioInputId playerAudioInput = NowSoundLib.AudioInputId.AudioInput1 + playerIndex;

            if (SoundManager.Instance != null)
            {
                for (int i = 0; i < state.EffectLevels.Length; i++)
                {
                    // Each time we remove one the indices of the later ones change.
                    // So just remove index 1 over and over.
                    // TODO: really use stable IDs if it makes sense to do so later (e.g. direct instance manipulation from the app).
                    // TODO: ...or just add a plugin instance query API to NowSoundLib and ask NowSoundLib what's the deal.
                    NowSoundGraphAPI.DeleteInputPluginInstance(playerAudioInput, (PluginInstanceIndex)1);
                }
            }

            // and update the state!!!
            state.Effects = new int[0];
            state.EffectLevels = new int[0];
            this.state = state;
        }
    }
}
