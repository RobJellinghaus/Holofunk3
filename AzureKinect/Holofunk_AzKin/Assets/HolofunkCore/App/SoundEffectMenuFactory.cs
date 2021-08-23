// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Sound;
using System;
using Holofunk.Menu;
using Holofunk.Distributed;
using Holofunk.Perform;
using System.Collections.Generic;
using Holofunk.Loop;
using System.Linq;
using Distributed.State;
using Holofunk.Core;

namespace Holofunk.App
{
    /// <summary>
    /// Creates MenuStructure corresponding to the sound effects menu.
    /// </summary>
    /// <remarks>
    /// This is in the Holofunk.App namespace because it refers to many different packages to implement its
    /// functionality, and we want the Holofunk.Menu namespace to not include all these other dependencies.
    /// 
    /// The state of the sound effects is determined by iterating over all instantiated DistributedSoundEffects.
    /// </remarks>
    public static class SoundEffectsMenuFactory
    {
        /// <summary>
        /// Construct a MenuStructure for all known sound effects.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static MenuStructure Create()
        {
            // Collections of names for use in menu creation.
            Dictionary<PluginId, string> pluginNames = new Dictionary<PluginId, string>();
            Dictionary<EffectId, string> programNames = new Dictionary<EffectId, string>();
            Dictionary<PluginId, List<PluginProgramId>> plugins = new Dictionary<PluginId, List<PluginProgramId>>();
            
            // Traverse all known sound effects, building up the collection of names etc.
            foreach (DistributedSoundEffect effect in DistributedObjectFactory.FindComponentInstances<DistributedSoundEffect>(
                DistributedObjectFactory.DistributedType.SoundEffect, includeActivePrototype: false))
            {
                if (!pluginNames.TryGetValue(effect.PluginId, out string name))
                {
                    pluginNames[effect.PluginId] = effect.PluginName;
                }

                EffectId effectId = new EffectId(effect.PluginId, effect.PluginProgramId);
                if (!programNames.TryGetValue(effectId, out string programName))
                {
                    programNames[effectId] = effect.ProgramName;
                }

                if (!plugins.TryGetValue(effect.PluginId, out List<PluginProgramId> programs))
                {
                    programs = new List<PluginProgramId>();
                    plugins[effect.PluginId] = programs;
                }
                programs.Add(effect.PluginProgramId);
            }

            // The action which actually applies a sound effect to whatever's being touched (or not).
            // Note that this action is only ever called on the machine that owns the menu.
            // So if in practice some of the state being closed over (e.g. distributedPerformer) is null
            // on a proxy, that's fine, since the proxy will never call this action.
            Func<PluginId, PluginProgramId, Action<HashSet<DistributedId>>> setSoundEffectAction = (pluginId, programId) =>
            {
                return touchedLoopieIds =>
                {
                    DistributedPerformer distributedPerformer =
                        DistributedObjectFactory.FindPrototypeComponent<DistributedPerformer>(
                            DistributedObjectFactory.DistributedType.Performer);

                    // are we touching anything?
                    if (touchedLoopieIds.Count == 0)
                    {
                        // apply this effect to the performer
                        PerformerState state = distributedPerformer.GetPerformer();
                        int length = state.Effects == null ? 0 : state.Effects.Length;
                        int[] newEffects = new int[length + 2];
                        if (state.Effects != null)
                        {
                            state.Effects.CopyTo(newEffects, 0);
                        }
                        newEffects[length] = (int)pluginId.Value;
                        newEffects[length + 1] = (int)programId.Value;

                        state.Effects = newEffects;

                        HoloDebug.Log($"SoundEffectMenuFactory.setSoundEffectAction: applying effect to performer, pluginId {pluginId}, programId {programId}");
                        distributedPerformer.UpdatePerformer(state);
                    }
                    else
                    {
                        // append this effect to all loopies being touched.
                        foreach (DistributedLoopie loopie in
                            DistributedObjectFactory.FindComponentInstances<DistributedLoopie>(
                                DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
                        {
                            if (touchedLoopieIds.Contains(loopie.Id))
                            {
                                HoloDebug.Log($"SoundEffectMenuFactory.setSoundEffectAction: applying effect to loopie {loopie.Id}, pluginId {pluginId}, programId {programId}");
                                loopie.AppendSoundEffect(new EffectId(pluginId, programId));
                            }
                        }
                    }
                };
            };

            List<(string, Action<HashSet<DistributedId>>, MenuStructure)> pluginItems = new List<(string, Action<HashSet<DistributedId>>, MenuStructure)>();
            pluginItems.Add((
                "Clear Effects",
                touchedLoopieIds =>
                {
                    DistributedPerformer distributedPerformer =
                        DistributedObjectFactory.FindPrototypeComponent<DistributedPerformer>(
                            DistributedObjectFactory.DistributedType.Performer);

                    if (touchedLoopieIds.Count > 0)
                    {
                        // apply this effect to the performer
                        PerformerState state = distributedPerformer.GetPerformer();
                        int[] newEffects = new int[0];
                        state.Effects = newEffects;
                        distributedPerformer.UpdatePerformer(state);
                    }
                    else
                    {
                        foreach (DistributedLoopie loopie in
                            DistributedObjectFactory.FindComponentInstances<DistributedLoopie>(
                                DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
                        {
                            if (touchedLoopieIds.Contains(loopie.Id))
                            {
                                loopie.ClearSoundEffects();
                            }
                        }
                    }
                },
                null));

            foreach (PluginId plid in pluginNames.Keys.OrderBy(k => k.Value))
            {
                // for lambda capturing
                PluginId pluginId = plid;
                List<PluginProgramId> childPrograms = plugins[pluginId];
                List<(string, Action<HashSet<DistributedId>>, MenuStructure)> items = new List<(string, Action<HashSet<DistributedId>>, MenuStructure)>();
                foreach (PluginProgramId prid in childPrograms)
                {
                    PluginProgramId pluginProgramId = prid;
                    EffectId effectId = new EffectId(pluginId, pluginProgramId);
                    string label = programNames[effectId];
                    items.Add((label, setSoundEffectAction(pluginId, pluginProgramId), null));
                }

                MenuStructure subMenu = new MenuStructure(items.ToArray());

                string pluginName = pluginNames[pluginId];
                pluginItems.Add((pluginName, null, subMenu));
            }

            return new MenuStructure(pluginItems.ToArray());
        }
    }
}
