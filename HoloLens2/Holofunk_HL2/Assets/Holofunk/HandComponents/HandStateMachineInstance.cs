﻿/// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Hand;
using Holofunk.Loop;
using Holofunk.Menu;
using Holofunk.Perform;
using Holofunk.StateMachines;
using Holofunk.Viewpoint;
using NowSoundLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.HandComponents
{
    using HandState = State<HandPoseEvent, HandController, HandController>;
    using HandAction = Action<HandPoseEvent, HandController>;
    //using HandToHandMenuState = State<HandPoseEvent, MenuModel<HandController>, HandController>;

    class HandStateMachine : StateMachine<HandPoseEvent>
    {
        static HandStateMachine s_instance;

        internal static HandStateMachine Instance
        {
            get
            {
                // on-demand initialization ensures no weirdness about static initializer ordering
                if (s_instance == null)
                {
                    s_instance = MakeHandStateMachine();
                }
                return s_instance;
            }
        }

        // convenience method to reduce typing at call sites
        static void AddTransition<TModel>(HandStateMachine ret, State<HandPoseEvent, TModel> from, HandPoseEvent evt, State<HandPoseEvent> to)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<HandPoseEvent, TModel>(evt, to));
        }

        // convenience method to reduce typing at call sites
        static void AddTransition<TModel>(HandStateMachine ret, State<HandPoseEvent, TModel> from, HandPoseEvent evt, State<HandPoseEvent> to, Func<bool> guardFunc)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<HandPoseEvent, TModel>(evt, to, guardFunc));
        }

        static void AddTransition<TModel>(HandStateMachine ret, State<HandPoseEvent, TModel> from, HandPoseEvent evt, Func<HandPoseEvent, TModel, Option<State<HandPoseEvent>>> computeTransitionFunc)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<HandPoseEvent, TModel>(evt, computeTransitionFunc));
        }

        HandStateMachine(HandState initialState, IComparer<HandPoseEvent> comparer)
            : base(initialState, comparer)
        {
        }

        // Set up the state machine we want for our dear little Loopies.
        static HandStateMachine MakeHandStateMachine()
        {
            ThreadContract.RequireUnity();

            HandState root = new HandState("root", null, new HandAction[0], new HandAction[0]);

            // Base state: just playing along.
            var initial = new HandState(
                "initial",
                root,
                (evt, handController) => { },
                (evt, handController) => { });

            var stateMachine = new HandStateMachine(initial, BodyPoseEventComparer.Instance);

            #region Recording

            HandState armed = new HandState(
                "armed",
                root,
                (evt, handController) => { },
                (evt, handController) => { });

            AddTransition(stateMachine, root, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, armed, HandPoseEvent.Unknown, initial);

            // We're recording.
            HandState recording = new HandState(
                "recording",
                armed,
                (evt, handController) =>
                {
                    // ignore hand position changes, to prevent them from kicking out of recording mode
                    handController.IgnoreHandPositionForHandPose = true;

                    //handController.PushSprite(SpriteId.HollowCircle, Color.red);

                    // Creating the loopie here assigns it as the currently held loopie.
                    // Note that this implicitly starts recording.
                    handController.CreateLoopie();
                },
                (evt, handController) =>
                {
                    // start paying attention to hand position again
                    handController.IgnoreHandPositionForHandPose = false;

                    //handController.PopGameObject();
                    handController.ReleaseLoopie();
                });

            // We're not recording; we're just showing a closed hand.
            HandState closedHand = new HandState(
                "closedHand",
                armed,
                (evt, handController) => { },
                (evt, handController) => { });

            // closing an open (armed) hand initiates new recording, if recording is enabled and UI didn't capture first
            AddTransition(
                stateMachine,
                armed,
                HandPoseEvent.Closed,
                // Start recording if and only if 1) the UI didn't capture this, and 2) recording is enabled.
                (evt, handController) => (!evt.IsCaptured /* && HolofunkController.Instance.IsRecordingEnabled */) ? recording : closedHand);

            AddTransition(stateMachine, recording, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, closedHand, HandPoseEvent.Opened, armed);

            #endregion

            #region Pointing

            // Super-state of all pointing states.  Exists to provide a single place for "unknown" hand pose to
            // be handled.
            HandState pointing = new HandState(
                "pointing",
                armed,
                (evt, handController) => { },
                (evt, handController) => { });

            AddTransition(stateMachine, pointing, HandPoseEvent.Unknown, initial);
            AddTransition(stateMachine, pointing, HandPoseEvent.Opened, armed);

            #endregion

            #region Mute/unmute

            // we're pointing, and about to possibly mute/unmute
            HandState pointingMuteUnmute = new HandState(
                "pointingMuteUnmute",
                pointing,
                (evt, handController) => { },
                (evt, handController) => { });

            AddTransition(stateMachine, armed, HandPoseEvent.Pointing1, pointingMuteUnmute);
            AddTransition(stateMachine, initial, HandPoseEvent.Pointing1, pointingMuteUnmute);

            HandState mute = new HandState(
                "mute",
                pointingMuteUnmute,
                (evt, handController) =>
                {
                    // initialize whether we are deleting the loopies we touch
                    Option<bool> deletingTouchedLoopies = Option<bool>.None;

                    // Collection of loopies that makes sure we don't flip loopies back and forth between states.
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    handController.touchedLoopieAction = loopie =>
                    {
                        HoloDebug.Log($"HandStateMachineInstance.Mute.TouchedLoopieAction: loopie {loopie.Id}, IsMuted {loopie.GetLoopie().IsMuted}");
                        // the first loopie touched, if it's a double-mute, puts us into delete mode
                        if (!deletingTouchedLoopies.HasValue)
                        {
                            deletingTouchedLoopies = loopie.GetLoopie().IsMuted;
                            HoloDebug.Log($"HandStateMachine.Mute.TouchedLoopieAction: deletingTouchedLoopies {deletingTouchedLoopies.Value}");
                        }

                        if (!toggledLoopies.Contains(loopie.Id))
                        {
                            toggledLoopies.Add(loopie.Id); // loopity doo, I've got another puzzle for you

                            // we know it has a value now
                            if (deletingTouchedLoopies.Value)
                            {
                                if (loopie.GetLoopie().IsMuted)
                                {
                                    HoloDebug.Log($"HandStateMachineInstance.Mute.TouchedLoopieAction: Deleting loopie {loopie.Id}");
                                    loopie.Delete();
                                }
                            }
                            else
                            {
                                HoloDebug.Log($"HandStateMachineInstance.Mute.TouchedLoopieAction: Setting loopie to mute: {loopie.Id}");
                                loopie.SetMute(true);
                            }
                        }
                    };
                },
                (evt, handController) => handController.touchedLoopieAction = null);

            AddTransition(stateMachine, pointingMuteUnmute, HandPoseEvent.Closed, mute);
            AddTransition(stateMachine, mute, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, mute, HandPoseEvent.Pointing1, pointingMuteUnmute);

            HandState unmute = new HandState(
                "unmute",
                pointingMuteUnmute,
                (evt, handController) =>
                {
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    handController.touchedLoopieAction = loopie =>
                    {
                        if (!toggledLoopies.Contains(loopie.Id))
                        {
                            toggledLoopies.Add(loopie.Id); // loopity doo, I've got another puzzle for you
                            loopie.SetMute(false);
                        }
                    };
                },
                (evt, handController) => handController.touchedLoopieAction = null);

            AddTransition(stateMachine, pointingMuteUnmute, HandPoseEvent.Opened, unmute);
            AddTransition(stateMachine, unmute, HandPoseEvent.Closed, initial);
            AddTransition(stateMachine, unmute, HandPoseEvent.Pointing1, pointingMuteUnmute);

            #endregion

            #region Effect popup menus

            /* original code:

            var effectPopupMenu = new HandToHandMenuState(
                "effectPopupMenu",
                pointing,
                (evt, handController) => { },
                (evt, handController) => { },
                entryConversionFunc: handController =>
                {
                    // ignore hand position changes, to prevent them from kicking out of popup mode
                    handController.IgnoreHandPositionForHandPose = true;
                    // keep the set of touched loopies stable, so whatever we originally touched is still what we apply sound effects to
                    handController.KeepTouchedLoopiesStable = true;

                    GameObject menuControllerGameObject;
                    MenuController menuController;
                    MenuController.Create(out menuControllerGameObject, out menuController);
                    List<MenuItem<HandController>> menuItems = new List<MenuItem<HandController>>();

                    menuItems.Add(new MenuItem<HandController>(
                        "WIPE FX",
                        _ => handController.RemoveAllSoundEffects()));

                    List<MenuItem<HandController>> volumeMenuSubitems = new List<MenuItem<HandController>>
                    {
                        new MenuItem<HandController>("Louder", _ => handController.ChangeVolume(louder: true)),
                        new MenuItem<HandController>("Softer", _ => handController.ChangeVolume(louder: false))
                    };

                    MenuItem<HandController> volumeMenu = new MenuItem<HandController>("Volume", subItems: volumeMenuSubitems);

                    menuItems.Add(volumeMenu);

                    for (int i = 0; i < HolofunkController.Instance.PluginPrograms.Count; i++)
                    {
                        List<string> pluginPrograms = HolofunkController.Instance.PluginPrograms[i];
                        List<MenuItem<HandController>> submenuItems = new List<MenuItem<HandController>>();
                        // look up the plugin's programs; only add menus for plugins with programs defined
                        if (pluginPrograms.Count > 0)
                        {
                            for (int j = 0; j < pluginPrograms.Count; j++)
                            {
                                // give loop variables their own locals, to ensure proper capture by the lambda below
                                int index = i;
                                int jndex = j; // forgive me
                                submenuItems.Add(new MenuItem<HandController>(
                                    pluginPrograms[jndex],
                                    hand => hand.ApplySoundEffectPluginProgram((PluginId)(index + 1), (ProgramId)(jndex + 1))));
                            }

                            menuItems.Add(new MenuItem<HandController>(HolofunkController.Instance.Plugins[i], subItems: submenuItems));
                        }
                    }

                    MenuModel<HandController> handMenuModel = new MenuModel<HandController>(
                        handController,
                        // exit action: delete menu when exiting this state
                        () => UnityEngine.Object.Destroy(menuControllerGameObject),
                        menuItems.ToArray());

                    menuController.Initialize(
                        handMenuModel,
                        handController,
                        handController.HandPosition);

                    return handMenuModel;
                },
                exitConversionFunc: menuModel =>
                {
                    HandController handController = menuModel.Exit();

                    // start paying attention to hand position again
                    handController.IgnoreHandPositionForHandPose = false;
                    // let loopies get (un)touched again
                    handController.KeepTouchedLoopiesStable = false;

                    return handController;
                }
                );

            AddTransition(stateMachine, effectPopupMenu, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, effectPopupMenu, HandPoseEvent.Closed, initial);

            // AddTransition(stateMachine, pointingMuteUnmute, BodyPoseEvent.OtherChest, effectPopupMenu);

            // Once we are effect dragging, we want to stay effect dragging, as it turns out.
            // AddTransition(ret, effectPopupMenu, LoopieEvent.OtherNeutral, pointingMuteUnmute);
            */

            #endregion

            #region System popup menu

            // State machine construction ensures there will only be one of these per hand state machine instance,
            // so we can safely use a local variable here to close over this menu
            GameObject menuGameObject = null;

            var systemPopupMenu = new HandState(
                "systemMenu",
                armed,
                (evt, handController) => {
                    handController.IgnoreHandPositionForHandPose = true;

                    // get the forward direction towards the camera from the hand location
                    PerformerState performerState = handController.DistributedPerformer.GetPerformer();
                    Vector3 localHandPosition = handController.HandPosition(ref performerState);

                    // get the performer's head position
                    Vector3 localHeadPosition = performerState.HeadPosition;

                    // we want a forward direction for the menu that orients it from unit Z towards localHeadPosition
                    Vector3 localHandToHeadDirection = (localHeadPosition - localHandPosition).normalized;

                    Vector3 viewpointHandPosition = DistributedViewpoint.Instance.LocalToViewpointMatrix()
                        .MultiplyPoint(localHandPosition);

                    Vector3 viewpointForwardDirection = DistributedViewpoint.Instance.LocalToViewpointMatrix()
                        .MultiplyVector(localHandToHeadDirection);

                    menuGameObject = DistributedMenu.Create(
                        MenuKinds.System,
                        viewpointForwardDirection,
                        viewpointHandPosition);
                },
                (evt, handController) => {
                    handController.IgnoreHandPositionForHandPose = false;

                    DistributedMenu menu = menuGameObject.GetComponent<DistributedMenu>();
                    if (evt == HandPoseEvent.Closed)
                    {
                        menu.InvokeSelectedAction();
                    }

                    if (menuGameObject != null)
                    {
                        UnityEngine.GameObject.Destroy(menuGameObject);
                    }
                });

            AddTransition(stateMachine, armed, HandPoseEvent.Pointing2, systemPopupMenu);
            AddTransition(stateMachine, initial, HandPoseEvent.Pointing2, systemPopupMenu);
            AddTransition(stateMachine, systemPopupMenu, HandPoseEvent.Opened, armed);
            AddTransition(stateMachine, systemPopupMenu, HandPoseEvent.Closed, initial);



            /* original code:

            var systemPopupMenu = new HandToHandMenuState(
                "systemPopupMenu",
                pointing,
                (evt, handController) => { },
                (evt, handController) => { },
                entryConversionFunc: handController =>
                {
                    // ignore hand position changes, to prevent them from kicking out of popup mode
                    handController.IgnoreHandPositionForHandPose = true;

                    bool areAnyLoopiesMine = false;
                    LoopieController.Apply(loopie =>
                    {
                        if (loopie.CreatorPlayerIndex == handController.PlayerIndex)
                        {
                            areAnyLoopiesMine = true;
                        }
                    });

                    // Parent the menu in world space so it will hold still.
                    GameObject menuControllerGameObject = GameObject.Instantiate(
                        GameObject.Find(nameof(MenuController)),
                        handController.transform.parent.parent);

                    MenuController menuController = menuControllerGameObject.GetComponent<MenuController>();

                    Action<HandController> deleteAction = _ =>
                    {
                        LoopieController.Apply(loopie =>
                        {
                            if (!areAnyLoopiesMine || loopie.CreatorPlayerIndex == handController.PlayerIndex)
                            {
                                loopie.Delete();
                            }
                        });
                    };

                    MenuItem<HandController> deleteSoundsItem = new MenuItem<HandController>(
                        (string)(areAnyLoopiesMine ? "Delete my sounds" : "Delete ALL sounds"),
                        deleteAction,
                        null);

                    MenuModel<HandController> handMenuModel = new MenuModel<HandController>(
                        handController,
                        () => UnityEngine.Object.Destroy(menuControllerGameObject),
                        deleteSoundsItem,
                        new MenuItem<HandController>(
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
                        new MenuItem<HandController>("Slide -1",
                            _ => GUIController.Instance.MoveSlide(-1)),
                        new MenuItem<HandController>(GUIController.Instance.IsSlideVisible ? "Hide slide" : "Show slide",
                            _ => GUIController.Instance.SetSlideVisible(!GUIController.Instance.IsSlideVisible)),
                        new MenuItem<HandController>("Slide +1",
                            _ => GUIController.Instance.MoveSlide(+1)),
                        new MenuItem<HandController>(GUIController.Instance.IsStatusTextVisible ? "Hide status text" : "Show status text",
                            _ => GUIController.Instance.SetStatusTextVisible(!GUIController.Instance.IsStatusTextVisible)),
                        new MenuItem<HandController>(
                            "+10 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute + 10),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<HandController>(
                            "-10 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute - 10),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<HandController>(
                            "+1 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute + 1),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<HandController>(
                            "-1 BPM",
                            _ => NowSoundGraphAPI.SetBeatsPerMinute(NowSoundGraphAPI.TimeInfo().BeatsPerMinute - 1),
                            enabledFunc: _ => !LoopieController.AnyLoopiesExist),
                        new MenuItem<HandController>(
                            handController.playerController.showBones ? "Hide bones" : "Show bones",
                            _ => handController.playerController.showBones = !handController.playerController.showBones)
                        );

                    menuController.Initialize(
                        handMenuModel,
                        handController,
                        handController.HandPosition);

                    return handMenuModel;

                },
                exitConversionFunc: menuModel =>
                {
                    HandController handController = menuModel.Exit();
                    // start paying attention to hand position again
                    handController.IgnoreHandPositionForHandPose = false;
                    return handController;
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

            return stateMachine;
        }
    }
}