/// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Hand;
using Holofunk.Loop;
using Holofunk.Menu;
using Holofunk.Perform;
using Holofunk.Shape;
using Holofunk.StateMachines;
using Holofunk.Viewpoint;
using Holofunk.VolumeWidget;
using NowSoundLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Controller
{
    using ControllerState = State<JoyconEvent, JoyconController, JoyconController>;
    using ControllerAction = Action<JoyconEvent, JoyconController>;
 
    class ControllerStateMachine : StateMachine<JoyconEvent>
    {
        static ControllerStateMachine s_instance;

        internal static ControllerStateMachine Instance
        {
            get
            {
                // on-demand initialization ensures no weirdness about static initializer ordering
                if (s_instance == null)
                {
                    s_instance = MakeControllerStateMachine();
                }
                return s_instance;
            }
        }

        // convenience method to reduce typing at call sites
        static void AddTransition<TModel>(ControllerStateMachine ret, State<JoyconEvent, TModel> from, JoyconEvent evt, State<JoyconEvent> to)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<JoyconEvent, TModel>(evt, to));
        }

        // convenience method to reduce typing at call sites
        static void AddTransition<TModel>(ControllerStateMachine ret, State<JoyconEvent, TModel> from, JoyconEvent evt, State<JoyconEvent> to, Func<bool> guardFunc)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<JoyconEvent, TModel>(evt, to, guardFunc));
        }

        static void AddTransition<TModel>(ControllerStateMachine ret, State<JoyconEvent, TModel> from, JoyconEvent evt, Func<JoyconEvent, TModel, Option<State<JoyconEvent>>> computeTransitionFunc)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<JoyconEvent, TModel>(evt, computeTransitionFunc));
        }

        ControllerStateMachine(ControllerState initialState, IComparer<JoyconEvent> comparer)
            : base(initialState, comparer)
        {
        }

        // Set up the state machine we want for our dear little Loopies.
        static ControllerStateMachine MakeControllerStateMachine()
        {
            ThreadContract.RequireUnity();

            ControllerState root = new ControllerState("root", null, new ControllerAction[0], new ControllerAction[0]);

            // Base state: just playing along.
            var initial = new ControllerState(
                "initial",
                root,
                (evt, joyconController) => { },
                (evt, joyconController) => { });

            var stateMachine = new ControllerStateMachine(initial, JoyconEventComparer.Instance);

            #region Recording

            // We're recording.
            ControllerState recording = new ControllerState(
                "recording",
                initial,
                (evt, joyconController) =>
                {
                    //joyconController.PushSprite(SpriteId.HollowCircle, Color.red);

                    // Creating the loopie here assigns it as the currently held loopie.
                    // Note that this implicitly starts recording.
                    joyconController.CreateLoopie();
                },
                (evt, joyconController) =>
                {
                    //joyconController.PopGameObject();
                    joyconController.ReleaseLoopie();
                });

            AddTransition(
                stateMachine,
                initial,
                JoyconEvent.TriggerPressed,
                // Start recording if and only if 1) the UI didn't capture this, and 2) recording is enabled.
                (evt, joyconController) => (!evt.IsCaptured /* && HolofunkController.Instance.IsRecordingEnabled */) ? recording : initial);

            AddTransition(stateMachine, recording, JoyconEvent.TriggerReleased, initial);

            #endregion

#if COMMENTED_OUT

            #region Pointing

            // Super-state of all pointing states.  Exists to provide a single place for "unknown" hand pose to
            // be handled.
            ControllerState pointing = new ControllerState(
                "pointing",
                armed,
                (evt, joyconController) => { },
                (evt, joyconController) => { });

            AddTransition(stateMachine, pointing, JoyconEvent.Unknown, initial);
            AddTransition(stateMachine, pointing, JoyconEvent.Opened, armed);

            // EXPERIMENT: don't make flat leave pointing
            AddTransition(stateMachine, pointing, JoyconEvent.Flat, pointing);

            #endregion

            #region Mute/unmute

            // we're pointing, and about to possibly mute/unmute
            ControllerState pointingMuteUnmute = new ControllerState(
                "pointingMuteUnmute",
                pointing,
                (evt, joyconController) => { },
                (evt, joyconController) => { });

            AddTransition(stateMachine, armed, JoyconEvent.Pointing1, pointingMuteUnmute);
            AddTransition(stateMachine, initial, JoyconEvent.Pointing1, pointingMuteUnmute);

            ControllerState mute = new ControllerState(
                "mute",
                pointingMuteUnmute,
                (evt, joyconController) =>
                {
                    // initialize whether we are deleting the loopies we touch
                    Option<bool> deletingTouchedLoopies = Option<bool>.None;

                    // Collection of loopies that makes sure we don't flip loopies back and forth between states.
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    joyconController.SetTouchedLoopieAction(loopie =>
                    {
                        HoloDebug.Log($"ControllerStateMachineInstance.Mute.TouchedLoopieAction: loopie {loopie.Id}, IsMuted {loopie.GetLoopie().IsMuted}");
                        // the first loopie touched, if it's a double-mute, puts us into delete mode
                        if (!deletingTouchedLoopies.HasValue)
                        {
                            deletingTouchedLoopies = loopie.GetLoopie().IsMuted;
                            HoloDebug.Log($"ControllerStateMachine.Mute.TouchedLoopieAction: deletingTouchedLoopies {deletingTouchedLoopies.Value}");
                        }

                        if (!toggledLoopies.Contains(loopie.Id))
                        {
                            toggledLoopies.Add(loopie.Id); // loopity doo, I've got another puzzle for you

                            // we know it has a value now
                            if (deletingTouchedLoopies.Value)
                            {
                                if (loopie.GetLoopie().IsMuted)
                                {
                                    HoloDebug.Log($"ControllerStateMachineInstance.Mute.TouchedLoopieAction: Deleting loopie {loopie.Id}");
                                    loopie.Delete();
                                }
                            }
                            else
                            {
                                HoloDebug.Log($"ControllerStateMachineInstance.Mute.TouchedLoopieAction: Setting loopie to mute: {loopie.Id}");
                                loopie.SetMute(true);
                            }
                        }
                    });
                },
                (evt, joyconController) => joyconController.SetTouchedLoopieAction(null));

            AddTransition(stateMachine, pointingMuteUnmute, JoyconEvent.Closed, mute);
            AddTransition(stateMachine, mute, JoyconEvent.Opened, armed);
            AddTransition(stateMachine, mute, JoyconEvent.Pointing1, pointingMuteUnmute);

            ControllerState unmute = new ControllerState(
                "unmute",
                pointingMuteUnmute,
                (evt, joyconController) =>
                {
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    joyconController.SetTouchedLoopieAction(loopie =>
                    {
                        if (!toggledLoopies.Contains(loopie.Id))
                        {
                            toggledLoopies.Add(loopie.Id); // loopity doo, I've got another puzzle for you
                            loopie.SetMute(false);
                        }
                    });
                },
                (evt, joyconController) => joyconController.SetTouchedLoopieAction(null));

            AddTransition(stateMachine, pointingMuteUnmute, JoyconEvent.Opened, unmute);
            AddTransition(stateMachine, unmute, JoyconEvent.Closed, initial);
            AddTransition(stateMachine, unmute, JoyconEvent.Pointing1, pointingMuteUnmute);

            #endregion

            #region Louden/soften

            // Shared state machine storage for the volume widget that visualizes changing the volume.
            DistributedVolumeWidget widget = null;

            // we're pointing, and about to possibly mute/unmute
            ControllerState loudenSoften = new ControllerState(
                "loudenSoften",
                armed,
                (evt, joyconController) =>
                {
                    // keep the set of touched loopies stable, so whatever we originally touched is still what we louden/soften
                    joyconController.KeepTouchedLoopiesStable = true;

                    widget = joyconController.CreateVolumeWidget().GetComponent<DistributedVolumeWidget>();
                    float initialHandYPosition = joyconController.GetLocalHandPosition().y;
                    float lastVolumeRatio = 1;
                    float volumeRatio = 1;

                    joyconController.SetUpdateAction(() =>
                    {
                        lastVolumeRatio = volumeRatio;

                        float currentHandYPosition = joyconController.GetLocalHandPosition().y;

                        float currentRatioOfMaxDistance = (currentHandYPosition - initialHandYPosition) / MagicNumbers.MaxVolumeHeightMeters;
                        // clamp this to (-1, 1) interval
                        currentRatioOfMaxDistance = Math.Min(1f, Math.Max(-1f, currentRatioOfMaxDistance));

                        float newRatio;
                        if (currentRatioOfMaxDistance > 0)
                        {
                            // map to interval (1, MaxVolumeRatio)
                            newRatio = 1 + (currentRatioOfMaxDistance * (MagicNumbers.MaxVolumeRatio - 1));
                        }
                        else
                        {
                            // map to interval (1/MaxVolumeRatio, 1)
                            newRatio = 1 / (1 - (currentRatioOfMaxDistance * (MagicNumbers.MaxVolumeRatio - 1)));
                        }

                        HoloDebug.Log($"ControllerStateMachineInstance.LouderSofter: initialHandY {initialHandYPosition}, currentHandY {currentHandYPosition}, currentRatioOfMax {currentRatioOfMaxDistance}, lastVolRatio {lastVolumeRatio}, newRatio {newRatio}");

                        volumeRatio = newRatio;

                        Core.Contract.Assert(!float.IsNaN(volumeRatio));
                        Core.Contract.Assert(!float.IsInfinity(volumeRatio));

                        VolumeWidgetState state = widget.State;
                        widget.UpdateState(
                            new VolumeWidgetState { ViewpointPosition = state.ViewpointPosition, VolumeRatio = volumeRatio });
                    });

                    joyconController.SetTouchedLoopieAction(loopie =>
                    {
                        if (lastVolumeRatio != volumeRatio)
                        {
                            loopie.MultiplyVolume(volumeRatio / lastVolumeRatio);
                        }
                    });
                },
                (evt, joyconController) =>
                {
                    // technically this should revert to whatever it was before, but we know in this case this was false before
                    joyconController.KeepTouchedLoopiesStable = false;

                    joyconController.SetUpdateAction(null);
                    joyconController.SetTouchedLoopieAction(null);

                    widget.Delete();
                });

            AddTransition(stateMachine, initial, JoyconEvent.Flat, loudenSoften);
            AddTransition(stateMachine, armed, JoyconEvent.Flat, loudenSoften);
            AddTransition(stateMachine, loudenSoften, JoyconEvent.Pointing1, loudenSoften);
            AddTransition(stateMachine, loudenSoften, JoyconEvent.Opened, loudenSoften);
            AddTransition(stateMachine, loudenSoften, JoyconEvent.Closed, initial);

            #endregion

            #region Effect popup menus

            // State machine construction ensures there will only be one of these per hand state machine instance,
            // so we can safely use a local variable here to close over this menu
            GameObject menuGameObject = null;

            var soundEffectMenu = new ControllerState(
                "soundEffectMenu",
                armed,
                (evt, joyconController) =>
                {
                    // keep the set of touched loopies stable, so whatever we originally touched is still what we apply sound effects to
                    joyconController.KeepTouchedLoopiesStable = true;

                    menuGameObject = CreateMenu(joyconController, MenuKinds.SoundEffects);
                    menuGameObject.GetComponent<MenuController>().Initialize(joyconController);
                },
                (evt, joyconController) => {
                    // let loopies get (un)touched again
                    joyconController.KeepTouchedLoopiesStable = false;

                    HashSet<DistributedId> touchedLoopies = new HashSet<DistributedId>(joyconController.TouchedLoopieIds);

                    DistributedMenu menu = menuGameObject.GetComponent<DistributedMenu>();
                    if (evt == JoyconEvent.Closed || evt == JoyconEvent.ThumbsUp)
                    {
                        HoloDebug.Log($"ControllerStateMachineInstance.soundEffectPopupMenu.exit: calling menu action on {touchedLoopies.Count} loopies");
                        menu.InvokeSelectedAction(touchedLoopies);
                    }

                    // delete it in the distributed sense.
                    // note that locally, this will synchronously destroy the game object
                    HoloDebug.Log($"ControllerStateMachineInstance.soundEffectPopupMenu.exit: deleting menu {menu.Id}");
                    menu.Delete();
                });

            AddTransition(stateMachine, armed, JoyconEvent.Bloom, soundEffectMenu);
            AddTransition(stateMachine, soundEffectMenu, JoyconEvent.Opened, armed);
            AddTransition(stateMachine, soundEffectMenu, JoyconEvent.Closed, initial);
            // debatable transition... but thumbs up looks like an intermediate pose on the way from bloom to closed
            AddTransition(stateMachine, soundEffectMenu, JoyconEvent.ThumbsUp, initial);
            AddTransition(stateMachine, soundEffectMenu, JoyconEvent.Flat, initial); // flaky recognition
            AddTransition(stateMachine, soundEffectMenu, JoyconEvent.Pointing1, pointingMuteUnmute);

            #endregion

#endif // COMMENTED_OUT

#if ORIGINAL_CODE

            #region Effect popup menu

            var effectPopupMenu = new HandToHandMenuState(
                "effectPopupMenu",
                pointing,
                (evt, joyconController) => { },
                (evt, joyconController) => { },
                entryConversionFunc: joyconController =>
                {
                    // ignore hand position changes, to prevent them from kicking out of popup mode
                    joyconController.IgnoreHandPositionForHandPose = true;
                    // keep the set of touched loopies stable, so whatever we originally touched is still what we apply sound effects to
                    joyconController.KeepTouchedLoopiesStable = true;

                    GameObject menuControllerGameObject;
                    MenuController menuController;
                    MenuController.Create(out menuControllerGameObject, out menuController);
                    List<MenuItem<JoyconController>> menuItems = new List<MenuItem<JoyconController>>();

                    menuItems.Add(new MenuItem<JoyconController>(
                        "WIPE FX",
                        _ => joyconController.RemoveAllSoundEffects()));

                    List<MenuItem<JoyconController>> volumeMenuSubitems = new List<MenuItem<JoyconController>>
                    {
                        new MenuItem<JoyconController>("Louder", _ => joyconController.ChangeVolume(louder: true)),
                        new MenuItem<JoyconController>("Softer", _ => joyconController.ChangeVolume(louder: false))
                    };

                    MenuItem<JoyconController> volumeMenu = new MenuItem<JoyconController>("Volume", subItems: volumeMenuSubitems);

                    menuItems.Add(volumeMenu);

                    for (int i = 0; i < HolofunkController.Instance.PluginPrograms.Count; i++)
                    {
                        List<string> pluginPrograms = HolofunkController.Instance.PluginPrograms[i];
                        List<MenuItem<JoyconController>> submenuItems = new List<MenuItem<JoyconController>>();
                        // look up the plugin's programs; only add menus for plugins with programs defined
                        if (pluginPrograms.Count > 0)
                        {
                            for (int j = 0; j < pluginPrograms.Count; j++)
                            {
                                // give loop variables their own locals, to ensure proper capture by the lambda below
                                int index = i;
                                int jndex = j; // forgive me
                                submenuItems.Add(new MenuItem<JoyconController>(
                                    pluginPrograms[jndex],
                                    hand => hand.ApplySoundEffectPluginProgram((PluginId)(index + 1), (ProgramId)(jndex + 1))));
                            }

                            menuItems.Add(new MenuItem<JoyconController>(HolofunkController.Instance.Plugins[i], subItems: submenuItems));
                        }
                    }

                    MenuModel<JoyconController> handMenuModel = new MenuModel<JoyconController>(
                        joyconController,
                        // exit action: delete menu when exiting this state
                        () => UnityEngine.Object.Destroy(menuControllerGameObject),
                        menuItems.ToArray());

                    menuController.Initialize(
                        handMenuModel,
                        joyconController,
                        joyconController.HandPosition);

                    return handMenuModel;
                },
                exitConversionFunc: menuModel =>
                {
                    JoyconController joyconController = menuModel.Exit();

                    // start paying attention to hand position again
                    joyconController.IgnoreHandPositionForHandPose = false;
                    // let loopies get (un)touched again
                    joyconController.KeepTouchedLoopiesStable = false;

                    return joyconController;
                }
                );

            AddTransition(stateMachine, effectPopupMenu, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, effectPopupMenu, HandPoseEvent.Closed, initial);

            // AddTransition(stateMachine, pointingMuteUnmute, BodyPoseEvent.OtherChest, effectPopupMenu);

            // Once we are effect dragging, we want to stay effect dragging, as it turns out.
            // AddTransition(ret, effectPopupMenu, LoopieEvent.OtherNeutral, pointingMuteUnmute);

            #endregion // Effect popup menu

            #region System popup menu

            var systemPopupMenu = new ControllerState(
                "systemMenu",
                armed,
                (evt, joyconController) =>
                {
                    menuGameObject = CreateMenu(joyconController, MenuKinds.System);
                    menuGameObject.GetComponent<MenuController>().Initialize(joyconController);
                },
                (evt, joyconController) => {
                    DistributedMenu menu = menuGameObject.GetComponent<DistributedMenu>();
                    if (evt == JoyconEvent.Closed)
                    {
                        // we don't pass specific affected objects to the system menu actions (yet...?).
                        menu.InvokeSelectedAction(null);
                    }

                    // delete it in the distributed sense.
                    // note that locally, this will synchronously destroy the game object
                    HoloDebug.Log($"ControllerStateMachineInstance.systemPopupMenu.exit: deleting menu {menu.Id}");
                    menu.Delete();
                });

            AddTransition(stateMachine, armed, JoyconEvent.ThumbsUp, systemPopupMenu);
            AddTransition(stateMachine, initial, JoyconEvent.ThumbsUp, systemPopupMenu);
            AddTransition(stateMachine, systemPopupMenu, JoyconEvent.Opened, armed);
            AddTransition(stateMachine, systemPopupMenu, JoyconEvent.Closed, initial);

            /* original code:

            var systemPopupMenu = new HandToHandMenuState(
                "systemPopupMenu",
                pointing,
                (evt, joyconController) => { },
                (evt, joyconController) => { },
                entryConversionFunc: joyconController =>
                {
                    // ignore hand position changes, to prevent them from kicking out of popup mode
                    joyconController.IgnoreHandPositionForHandPose = true;

                    bool areAnyLoopiesMine = false;
                    LoopieController.Apply(loopie =>
                    {
                        if (loopie.CreatorPlayerIndex == joyconController.PlayerIndex)
                        {
                            areAnyLoopiesMine = true;
                        }
                    });

                    // Parent the menu in world space so it will hold still.
                    GameObject menuControllerGameObject = GameObject.Instantiate(
                        GameObject.Find(nameof(MenuController)),
                        joyconController.transform.parent.parent);

                    MenuController menuController = menuControllerGameObject.GetComponent<MenuController>();

                    Action<JoyconController> deleteAction = _ =>
                    {
                        LoopieController.Apply(loopie =>
                        {
                            if (!areAnyLoopiesMine || loopie.CreatorPlayerIndex == joyconController.PlayerIndex)
                            {
                                loopie.Delete();
                            }
                        });
                    };

                    MenuItem<JoyconController> deleteSoundsItem = new MenuItem<JoyconController>(
                        (string)(areAnyLoopiesMine ? "Delete my sounds" : "Delete ALL sounds"),
                        deleteAction,
                        null);

                    MenuModel<JoyconController> handMenuModel = new MenuModel<JoyconController>(
                        joyconController,
                        () => UnityEngine.Object.Destroy(menuControllerGameObject),
                        deleteSoundsItem,
                        new MenuItem<JoyconController>(
                            HolofunkController.Instance.IsRecordingToFile ? "Stop WAV recording" : "Start WAV recording",
                            _ =>
                            {
                                if (HolofunkController.Instance.IsRecordingToFile)
                                {
                                    HolofunkController.Instance.StopRecording();
                                }
                                else
                                {
                                    HolofunkController.Instance.StartRecording();
                                }
                            }),
                        new MenuItem<JoyconController>("Slide -1",
                            _ => GUIController.Instance.MoveSlide(-1)),
                        new MenuItem<JoyconController>(GUIController.Instance.IsSlideVisible ? "Hide slide" : "Show slide",
                            _ => GUIController.Instance.SetSlideVisible(!GUIController.Instance.IsSlideVisible)),
                        new MenuItem<JoyconController>("Slide +1",
                            _ => GUIController.Instance.MoveSlide(+1)),
                        new MenuItem<JoyconController>(GUIController.Instance.IsStatusTextVisible ? "Hide status text" : "Show status text",
                            _ => GUIController.Instance.SetStatusTextVisible(!GUIController.Instance.IsStatusTextVisible)),
                        new MenuItem<JoyconController>(
                            "+10 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute + 10),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<JoyconController>(
                            "-10 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute - 10),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<JoyconController>(
                            "+1 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute + 1),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<JoyconController>(
                            "-1 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute - 1),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<JoyconController>(
                            joyconController.playerController.showBones ? "Hide bones" : "Show bones",
                            _ => joyconController.playerController.showBones = !joyconController.playerController.showBones)
                        );

                    menuController.Initialize(
                        handMenuModel,
                        joyconController,
                        joyconController.HandPosition);

                    return handMenuModel;

                },
                exitConversionFunc: menuModel =>
                {
                    JoyconController joyconController = menuModel.Exit();
                    // start paying attention to hand position again
                    joyconController.IgnoreHandPositionForHandPose = false;
                    return joyconController;
                }
                );

            AddTransition(stateMachine, systemPopupMenu, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, systemPopupMenu, HandPoseEvent.Closed, initial);

            // These transitions are with respect to the *other* hand.  They may still be appropriate,
            // but we're not using them again quite yet, pending more experience.
            //AddTransition(stateMachine, pointingMuteUnmute, BodyPoseEvent.OverHead, systemPopupMenu);
            //AddTransition(stateMachine, effectPopupMenu, BodyPoseEvent.OverHead, systemPopupMenu);
            //AddTransition(stateMachine, systemPopupMenu, BodyPoseEvent.AtChest, effectPopupMenu);

            */

            #endregion

#endif // ORIGINAL_CODE

            return stateMachine;
        }

        private static GameObject CreateMenu(JoyconController joyconController, MenuKinds menuKind)
        {
            GameObject menuGameObject;
            // get the forward direction towards the camera from the hand location
            Vector3 localHandPosition = joyconController.GetViewpointHandPosition();

            // get the performer's head position
            Vector3 localHeadPosition = joyconController.GetViewpointHeadPosition();

            // default direction is BACKWARDS on Z so we actually want head facing towards hand vector
            Vector3 localHandToHeadDirection = (localHandPosition - localHeadPosition).normalized;

            // TODO: fix this up so Z is always towards viewpoint
            Vector3 viewpointHandPosition = DistributedViewpoint.Instance.LocalToViewpointMatrix()
                .MultiplyPoint(localHandPosition);

            Vector3 viewpointForwardDirection = DistributedViewpoint.Instance.LocalToViewpointMatrix()
                .MultiplyVector(localHandToHeadDirection);

            menuGameObject = DistributedMenu.Create(
                menuKind,
                viewpointForwardDirection,
                viewpointHandPosition);
            return menuGameObject;
        }
    }
}
