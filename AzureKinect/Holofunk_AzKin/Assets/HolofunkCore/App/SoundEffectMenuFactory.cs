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
    /// Action applied to the touched loopies to alter the level of a given effect.
    using AlterEffectLevelAction = Action<HashSet<DistributedId>, DistributedId, int, bool>;

    /// Function that takes an EffectId and an initial level, and returns an AlterEffectLevelAction.
    using AlterEffectFunc = Func<EffectId, Action<HashSet<DistributedId>, DistributedId, int, bool>>;

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
            AlterEffectFunc alterEffectFunc = effectId =>
            {
                return (touchedLoopieIds, performerId, alteredLevel, commit) =>
                {
                    DistributedPerformer distributedPerformer =
                        (DistributedPerformer)DistributedHoster.Host.Owners[performerId];

                    // are we touching anything?
                    if (touchedLoopieIds.Count == 0)
                    {
                        // apply this effect to the performer
                        distributedPerformer.AlterSoundEffect(effectId, 100, )

                        HoloDebug.Log($"SoundEffectMenuFactory.appendSoundEffectAction: after action, there are {fx.Length} effect ids");
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
                                HoloDebug.Log($"SoundEffectMenuFactory.appebdSoundEffectAction: applying effect to loopie {loopie.Id}, pluginId {pluginId}, programId {programId}");
                                loopie.AppendSoundEffect(new EffectId(pluginId, programId));
                            }
                        }
                    }
                };
            };

            List<(MenuVerb, MenuStructure)> menuItems = new List<(MenuVerb, MenuStructure)>();

            menuItems.Add(
                new MenuVerb(
                    MenuVerbKind.Prompt,
                    "Clear Effects",
                    touchedLoopieIds =>
                    {
                        DistributedPerformer distributedPerformer =
                            DistributedObjectFactory.FindPrototypeComponent<DistributedPerformer>(
                                DistributedObjectFactory.DistributedType.Performer);

                        if (touchedLoopieIds.Count == 0)
                        {
                            // apply this effect to the performer
                            PerformerState state = distributedPerformer.GetState();
                            int[] newEffects = new int[0];
                            state.Effects = newEffects;
                            distributedPerformer.UpdatePerformer(state);

                            HoloDebug.Log($"SoundEffectMenuFactory.clearSoundEffectAction: applied empty effects to performer with {distributedPerformer.GetState().Effects.Length} effect IDs now");
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

                            HoloDebug.Log($"SoundEffectMenuFactory.clearSoundEffectAction: cleared sound effects from {touchedLoopieIds.Count} loopies");
                        }
                    }),
                null);

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
                    items.Add((label, appendSoundEffectAction(pluginId, pluginProgramId), null));
                }

                MenuStructure subMenu = new MenuStructure(items.ToArray());

                string pluginName = pluginNames[pluginId];
                menuItems.Add((pluginName, null, subMenu));
            }

            return new MenuStructure(menuItems.ToArray());
        }
    }
}
