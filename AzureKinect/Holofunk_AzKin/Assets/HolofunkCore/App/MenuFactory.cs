// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Controller;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Loop;
using Holofunk.Menu;
using Holofunk.Perform;
using Holofunk.Sound;
using Holofunk.Viewpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Holofunk.Controller.ControllerStateMachine;

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

            Action<MenuVerbModel, HashSet<DistributedId>, bool> grabAction = (menuVerbModel, loopieIds, isCopy) =>
            {
                PPlusController controller = ((PPlusModel)menuVerbModel.Parent).Controller;
                Vector3 currentHandPosition = controller.GetViewpointHandPosition();
                Vector3 delta = currentHandPosition - menuVerbModel.LastViewpointHandPosition;

                // if we are copying and we don't have a set of copied loopie IDs yet, then create it and copy them all now
                if (isCopy)
                {
                    if (menuVerbModel.CopiedLoopieIds == null)
                    {
                        // let's copy them all! First collect the ones we'll be copying.
                        HashSet<DistributedLoopie> copiedLoopies =
                        DistributedObjectFactory.CollectDistributedComponents<DistributedLoopie>(
                            DistributedObjectFactory.DistributedType.Loopie,
                            new HashSet<DistributedId>(controller.TouchedLoopieIds));

                        // Now copy each one.
                        HashSet<DistributedId> newLoopieIds = new HashSet<DistributedId>();
                        foreach (DistributedLoopie copiedLoopie in copiedLoopies)
                        {
                            LocalLoopie localCopiedLoopie = (LocalLoopie)copiedLoopie.LocalObject;

                            GameObject newLoopie = DistributedLoopie.Create(
                                localCopiedLoopie.GetLoopie().ViewpointPosition,
                                NowSoundLib.AudioInputId.AudioInputUndefined,
                                copiedLoopie.Id,
                                localCopiedLoopie.GetLoopie().Effects,
                                localCopiedLoopie.GetLoopie().EffectLevels);

                            newLoopieIds.Add(newLoopie.GetComponent<DistributedLoopie>().Id);
                        }

                        // And update the list of copied ones so we can drag them around.
                        menuVerbModel.CopiedLoopieIds = newLoopieIds;
                    }

                    loopieIds = menuVerbModel.CopiedLoopieIds;
                }

                HashSet<DistributedLoopie> loopies =
                    DistributedObjectFactory.CollectDistributedComponents<DistributedLoopie>(
                        DistributedObjectFactory.DistributedType.Loopie,
                        loopieIds);

                foreach (DistributedLoopie loopie in loopies)
                {
                    loopie.SetViewpointPosition(loopie.GetLoopie().ViewpointPosition + delta);
                }

                menuVerbModel.SetLastViewpointHandPosition(currentHandPosition);
            };

            // Grab > move/copy
            items.Add((MenuVerb.MakeLabel("Grab"), new MenuStructure(
                (MenuVerb.MakeTouch("Move", (menuVerbModel, loopieIds) => grabAction(menuVerbModel, loopieIds, /*isCopy:*/ false)),
                 null),
                (MenuVerb.MakeTouch("Copy", (menuVerbModel, loopieIds) => grabAction(menuVerbModel, loopieIds, /*isCopy:*/ true)),
                 null))));

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
                        "Clear\nAll",
                        (_, effectableIds) =>
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
                        "Pop\nLast",
                        (_, effectableIds) =>
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
                                    HoloDebug.Log($"SoundEffectMenuFactory.levelAction: applying drywet level to effectable {asObj.Id} with alteration {alteration}");
                                    // OK, OK, we don't have real per-effect dry/wet yet but we kind of hack it by
                                    // just altering volume and effect on every move.
                                    effectable.AlterSoundEffect(new EffectId(pluginId, pluginProgramId), alteration, commit);
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
