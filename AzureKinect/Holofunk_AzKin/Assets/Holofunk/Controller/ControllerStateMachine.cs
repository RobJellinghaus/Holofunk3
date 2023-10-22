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
using Holofunk.LevelWidget;
using NowSoundLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Holofunk.Sound;

namespace Holofunk.Controller
{
    using ControllerState = State<PPlusEvent, PPlusModel, PPlusModel>;

    class ControllerStateMachine : StateMachine<PPlusEvent>
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
        static void AddTransition<TModel>(ControllerStateMachine ret, State<PPlusEvent, TModel> from, PPlusEvent evt, State<PPlusEvent> to)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<PPlusEvent, TModel>(evt, to));
        }

        // convenience method to reduce typing at call sites
        static void AddTransition<TModel>(ControllerStateMachine ret, State<PPlusEvent, TModel> from, PPlusEvent evt, State<PPlusEvent> to, Func<TModel, bool> guardFunc)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<PPlusEvent, TModel>(evt, to, guardFunc));
        }

        static void AddTransition<TModel>(ControllerStateMachine ret, State<PPlusEvent, TModel> from, PPlusEvent evt, Func<PPlusEvent, TModel, Option<State<PPlusEvent>>> computeTransitionFunc)
            where TModel : IModel
        {
            ret.AddTransition<TModel>(from, new Transition<PPlusEvent, TModel>(evt, computeTransitionFunc));
        }

        ControllerStateMachine(ControllerState initialState, IComparer<PPlusEvent> comparer)
            : base(initialState, comparer)
        {
        }

        internal class RecordingModel : ChildModel<RecordingModel, PPlusModel>
        {
            /// <summary>
            /// The loopie being recorded into.
            /// </summary>
            internal GameObject HeldLoopie { get; private set; }

            internal RecordingModel(PPlusModel parent, GameObject heldLoopie, Action<RecordingModel> updateAction)
                : base(parent, updateAction)
            {
                HeldLoopie = heldLoopie;
            }
        }

        /// <summary>
        /// Model for level adjustment: contains the adjustment value and the widget that's displaying the value.
        /// </summary>
        internal class LevelAdjustModel : ChildModel<LevelAdjustModel, PPlusModel>
        {
            /// <summary>
            /// The current adjustment.
            /// </summary>
            internal float Adjustment { get; set; }
            internal DistributedLevelWidget LevelWidget { get; private set; }

            internal LevelAdjustModel(PPlusModel parent, Action<LevelAdjustModel> updateAction, DistributedLevelWidget levelWidget)
                : base(parent, updateAction)
            {
                LevelWidget = levelWidget;
            }
        }

        /// <summary>
        /// Model for menu display.
        /// </summary>
        internal class MenuModel : ChildModel<MenuModel, PPlusModel>
        {
            /// <summary>
            /// The current adjustment.
            /// </summary>
            internal DistributedMenu Menu { get; private set; }

            internal MenuModel(PPlusModel parent, Action<MenuModel> updateAction, DistributedMenu menu)
                : base(parent, updateAction)
            {
                Menu = menu;
            }
        }

        // Set up the state machine we want for our dear little Loopies.
        static ControllerStateMachine MakeControllerStateMachine()
        {
            ThreadContract.RequireUnity();

            ControllerState root = new ControllerState(
                "root", 
                null,
                (evt, pplusController) => pplusController,
                (_1, _2) => { });

            // Base state: just playing along.
            var initial = new ControllerState(
                "initial",
                root,
                (evt, pplusController) => pplusController,
                (_1, _2) => { });

            var stateMachine = new ControllerStateMachine(initial, PPlusEventComparer.Instance);

            #region Recording

            // We're recording.
            State<PPlusEvent, RecordingModel, PPlusModel> recording = new State<PPlusEvent, RecordingModel, PPlusModel>(
                "recording",
                initial,
                (evt, pplusModel) =>
                {
                    //pplusController.PushSprite(SpriteId.HollowCircle, Color.red);

                    // Creating the loopie here assigns it as the currently held loopie.
                    // Note that this implicitly starts recording.
                    GameObject loopie = pplusModel.Controller.CreateLoopie(
                        pplusModel.Controller.playerIndex == 0
                            ? NowSoundLib.AudioInputId.AudioInput1
                            : NowSoundLib.AudioInputId.AudioInput2);

                    // update doesn't need to do anything while recording
                    return new RecordingModel(pplusModel, loopie, _ => { });
                },
                (_, recordingModel) =>
                {
                    recordingModel.HeldLoopie.GetComponent<DistributedLoopie>().FinishRecording();
                });

            AddTransition(
                stateMachine,
                initial,
                PPlusEvent.MikeDown,
                // Start recording if and only if 1) the UI didn't capture this, and 2) recording is enabled.
                (evt, pplusModel) => (!evt.IsCaptured /* && HolofunkController.Instance.IsRecordingEnabled */)
                    ? (State<PPlusEvent>)recording
                    : (State<PPlusEvent>)initial);

            AddTransition(stateMachine, recording, PPlusEvent.MikeUp, initial);

            #endregion

            #region Mute/unmute

            ControllerState mute = new ControllerState(
                "mute",
                initial,
                (evt, pplusModel) =>
                {
                    // initialize whether we are deleting the loopies we touch
                    Option<bool> deletingTouchedLoopies = Option<bool>.None;

                    // Collection of loopies that makes sure we don't flip loopies back and forth between states.
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    return new PPlusModel(
                        pplusModel.Controller,
                        pplusModel =>
                        {
                            pplusModel.Controller.UpdateTouchedLoopieList();
                            pplusModel.Controller.ApplyToTouchedLoopies(loopie =>
                            {
                                HoloDebug.Log($"ControllerStateMachine.Mute.TouchedLoopieAction: loopie {loopie.Id}, IsMuted {loopie.GetLoopie().IsMuted}");
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
                                            HoloDebug.Log($"ControllerStateMachine.Mute.TouchedLoopieAction: Deleting loopie {loopie.Id}");
                                            loopie.Delete();
                                        }
                                    }
                                    else
                                    {
                                        HoloDebug.Log($"ControllerStateMachine.Mute.TouchedLoopieAction: Setting loopie to mute: {loopie.Id}");
                                        loopie.SetMute(true);
                                    }
                                }
                            });
                        });
                },
                (_1, _2) => { });

            AddTransition(stateMachine, initial, PPlusEvent.LeftDown, mute);
            AddTransition(stateMachine, mute, PPlusEvent.LeftUp, initial);

            ControllerState unmute = new ControllerState(
                "unmute",
                initial,
                (evt, pplusModel) =>
                {
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    return new PPlusModel(
                        pplusModel.Controller,
                        pplusModel =>
                        {
                            pplusModel.Controller.UpdateTouchedLoopieList();
                            pplusModel.Controller.ApplyToTouchedLoopies(loopie =>
                            {
                                if (!toggledLoopies.Contains(loopie.Id))
                                {
                                    toggledLoopies.Add(loopie.Id); // loopity doo, I've got another puzzle for you
                                    loopie.SetMute(false);
                                }
                            });
                        });
                },
                (_1, _2) => { });

            AddTransition(stateMachine, initial, PPlusEvent.RightDown, unmute);
            AddTransition(stateMachine, unmute, PPlusEvent.RightUp, initial);

            #endregion

            #region Level change

            // Shared state machine storage for the level widget that visualizes changing the level.
            // NO NO NO DO NOT DO THIS IT GETS SHARED ACROSS ALL INSTANCES OF THE STATE MACHINE NOOOOOOOO
            // (it works only when there is exactly one state machine instance in existence)
            // DistributedLevelWidget widget = null;

            State<PPlusEvent, LevelAdjustModel, PPlusModel> levelChange = new State<PPlusEvent, LevelAdjustModel, PPlusModel>(
                "levelChange",
                initial,
                (evt, pplusModel) =>
                {
                    DistributedLevelWidget widget = pplusModel.Controller.CreateLevelWidget().GetComponent<DistributedLevelWidget>();
                    float initialHandYPosition = pplusModel.Controller.GetViewpointHandPosition().y;

                    return new LevelAdjustModel(
                        pplusModel,
                        levelModel =>
                        {
                            // DO NOT call UpdateTouchedLoopieList; we want the touched loopies to remain fixed.

                            float lastAdjustment = levelModel.Adjustment;

                            float currentHandYPosition = pplusModel.Controller.GetViewpointHandPosition().y;

                            float adjustment = (currentHandYPosition - initialHandYPosition) / MagicNumbers.MaxVolumeHeightMeters;
                            // clamp this to (-1, 1) interval
                            adjustment = Math.Min(1f, Math.Max(-1f, adjustment));

                            HoloDebug.Log($"ControllerStateMachineInstance.LouderSofter: initialHandY {initialHandYPosition}, currentHandY {currentHandYPosition}, adjustment {adjustment}, lastAdjustment {lastAdjustment}");

                            Core.Contract.Assert(!float.IsNaN(adjustment));
                            Core.Contract.Assert(!float.IsInfinity(adjustment));

                            levelModel.Adjustment = adjustment;

                            LevelWidgetState state = widget.State;
                            widget.UpdateState(
                                new LevelWidgetState { ViewpointPosition = state.ViewpointPosition, Adjustment = adjustment });

                            // TODO: actually apply the effects
                        },
                        widget);
                },
                (evt, levelAdjustModel) =>
                {
                    // TODO: actually commit the effects

                    levelAdjustModel.LevelWidget.Delete();
                });

            AddTransition(stateMachine, initial, PPlusEvent.LightDown, levelChange);
            AddTransition(stateMachine, levelChange, PPlusEvent.LightUp, initial);

            #endregion

            #region Effect popup menus

            var menu = new State<PPlusEvent, MenuModel, PPlusModel>(
                "Menu",
                initial,
                (evt, pplusModel) => new MenuModel(
                    pplusModel,
                    _ => { }, // nothing to do when updating; the MenuController gets its own Update call
                    pplusModel.Controller.CreateMenu().GetComponent<DistributedMenu>()),
                (evt, menuModel) => {
                    DistributedMenu menu = menuModel.Menu;
                    //HoloDebug.Log($"ControllerStateMachineInstance.systemMenu.exit: calling menu action on {touchedLoopies.Count} loopies");
                    // menu.InvokeSelectedAction();

                    // delete it in the distributed sense.
                    // note that locally, this will synchronously destroy the game object
                    HoloDebug.Log($"ControllerStateMachineInstance.systemMenu.exit: deleting menu {menu.Id}");
                    menu.Delete();
                });

            AddTransition(stateMachine, initial, PPlusEvent.TeamsDown, menu);
            AddTransition(stateMachine, menu, PPlusEvent.TeamsUp, initial);

            #endregion

            return stateMachine;
        }
    }
}
