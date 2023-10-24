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
using Holofunk.Viewpoint;

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
    public static class MenuFactory
    {
        /// <summary>
        /// Construct a MenuStructure for all known sound effects.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static MenuStructure Create()
        {
            List<(MenuVerb, MenuStructure)> items = new List<(MenuVerb, MenuStructure)>();

            // Construct system menu items
            {
                DistributedSoundClock clock1 = DistributedSoundClock.Instance;
                float bpm = clock1.TimeInfo.Value.BeatsPerMinute;

                bool isViewpoint = false;
                bool isRecording = false;
                if (DistributedViewpoint.Instance != null)
                {
                    isViewpoint = true;
                    isRecording = DistributedViewpoint.Instance.IsRecording;
                }

                Action<int> setBPMAction = delta =>
                {
                    DistributedSoundClock clock2 = DistributedSoundClock.Instance;
                    if (clock2 != null)
                    {
                        float newBPM = clock2.TimeInfo.Value.BeatsPerMinute + delta;
                        if (newBPM > 0)
                        {
                            clock2.SetBeatsPerMinute(newBPM);
                        }
                    }
                };

                Action deleteMySoundsAction = () =>
                {
                    // Collect all the loopie IDs first (don't enumerate while deleting)
                    HashSet<DistributedLoopie> loopies = new HashSet<DistributedLoopie>();
                    foreach (DistributedLoopie loopie in DistributedObjectFactory.FindComponentInstances<DistributedLoopie>(
                        DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
                    {
                        loopies.Add(loopie);
                    };
                    foreach (DistributedLoopie loopie in loopies)
                    {
                        // one-way, immediate message
                        loopie.Delete();
                    }
                };

                // The root menu item; enables "unselecting" the currently held verb, and (hackishly)
                // goes at the center.
                items.Add((MenuVerb.MakeRoot(), null));

                items.Add((MenuVerb.MakeLabel("BPM"), new MenuStructure(
                        (MenuVerb.MakePrompt($"BPM+10", () => setBPMAction(10)), null),
                        (MenuVerb.MakePrompt($"BPM-10", () => setBPMAction(-10)), null))));

                // TODO: add a both-arrow-buttons state that deletes all sounds
                items.Add((MenuVerb.MakePrompt("Delete\nMy Sounds", deleteMySoundsAction), null));

                if (isViewpoint)
                {
                    if (isRecording)
                    {
                        items.Add((MenuVerb.MakePrompt("Stop\nRecording", () => DistributedViewpoint.Instance.StopRecording()), null));
                    }
                    else
                    {
                        items.Add((MenuVerb.MakePrompt("Start\nRecording", () => DistributedViewpoint.Instance.StartRecording()), null));
                    }
                }

            }

            // Construct sound effect menu items
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

                items.Add((MenuVerb.MakeLabel("FX"), new MenuStructure(
                    (MenuVerb.MakeTouch(
                        "Clear\nAll FX",
                        effectableIds =>
                        {
                            foreach (IEffectable effectable in DistributedObjectFactory.FindComponentInterfaces())
                            {
                                IDistributedObject asObj = (IDistributedObject)effectable;
                                if (effectableIds.Contains(asObj.Id))
                                {
                                    effectable.ClearSoundEffects();
                                }
                            }
                        }),
                    null),
                    (MenuVerb.MakeTouch(
                        "Remove\nLast FX",
                        effectableIds =>
                        {
                            foreach (IEffectable effectable in DistributedObjectFactory.FindComponentInterfaces())
                            {
                                IDistributedObject asObj = (IDistributedObject)effectable;
                                if (effectableIds.Contains(asObj.Id))
                                {
                                    effectable.PopSoundEffect();
                                }
                            }
                        }),
                    null))));

                foreach (PluginId plid in pluginNames.Keys.OrderBy(k => k.Value))
                {
                    // for lambda capturing
                    PluginId pluginId = plid;
                    string pluginName = pluginNames[pluginId];
                    List<PluginProgramId> childPrograms = plugins[pluginId];
                    List<(MenuVerb, MenuStructure)> subItems = new List<(MenuVerb, MenuStructure)>();
                    foreach (PluginProgramId prid in childPrograms)
                    {
                        PluginProgramId pluginProgramId = prid;
                        EffectId effectId = new EffectId(pluginId, pluginProgramId);
                        string label = programNames[effectId];

                        // The action which actually applies a sound effect to whatever's being touched (or not).
                        // Note that this action is only ever called on the machine that owns the menu.
                        // So if in practice some of the state being closed over (e.g. distributedPerformer) is null
                        // on a proxy, that's fine, since the proxy will never call this action.
                        Action<HashSet<DistributedId>, float, bool> levelAction = (effectableIds, alteration, commit) =>
                        {
                            // append this effect to all effectables being touched.
                            foreach (IEffectable effectable in DistributedObjectFactory.FindComponentInterfaces())
                            {
                                IDistributedObject asObj = (IDistributedObject)effectable;
                                if (effectableIds.Contains(asObj.Id))
                                {
                                    HoloDebug.Log($"SoundEffectMenuFactory.levelAction: applying volume to effectable {asObj.Id} with alteration {alteration}");
                                    // OK, OK, we don't have real per-effect dry/wet yet but we kind of hack it by
                                    // just altering volume and effect on every move.
                                    effectable.AlterSoundEffect(new EffectId(pluginId, pluginProgramId), alteration, commit);
                                    effectable.AlterVolume(alteration, commit);
                                }
                            }
                        };

                        subItems.Add((MenuVerb.MakeLevel(label, true, levelAction), null));
                    }

                    MenuStructure subMenu = new MenuStructure(subItems.ToArray());

                    items.Add((MenuVerb.MakeLabel(pluginName), subMenu));
                }
            }

            return new MenuStructure(items.ToArray());
        }
    }
}
