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
using System.Linq;

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

        internal class RecordingModel : BaseModel<RecordingModel>
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
        internal class MenuVerbModel : BaseModel<MenuVerbModel>
        {
            /// <summary>
            /// The current adjustment.
            /// </summary>
            internal float Adjustment { get; set; }
            internal DistributedLevelWidget LevelWidget { get; private set; }

            internal MenuVerbModel(PPlusModel parent, Action<MenuVerbModel> updateAction, DistributedLevelWidget levelWidget)
                : base(parent, updateAction)
            {
                LevelWidget = levelWidget;
            }
        }

        /// <summary>
        /// Model for menu display.
        /// </summary>
        internal class MenuModel : BaseModel<MenuModel>
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
                (_, model) => model,
                (_1, _2) => { });

            // Base state: just playing along.
            var initial = new ControllerState(
                "initial",
                root,
                (evt, pplusModel) => new PPlusModel(
                    pplusModel,
                    pplusModel.Controller,
                    pplusModel => pplusModel.Controller.UpdateTouchedLoopieList()),
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
                    return new RecordingModel(
                        pplusModel,
                        loopie,
                        recordingModel => {
                            Vector3 viewpointHandPosition = pplusModel.Controller.GetViewpointHandPosition();
                            /*
                            Matrix4x4 localToViewpointMatrix = DistributedViewpoint.Instance.LocalToViewpointMatrix();
                            if (localToViewpointMatrix != Matrix4x4.zero)
                            {
                                Vector3 viewpointHandPosition = localToViewpointMatrix.MultiplyPoint(performerHandPosition);
                            }
                            */
                            //Debug.Log($"Updated viewport hand position of loopie {currentlyHeldLoopie} to {viewpointHandPosition}");
                            loopie.GetComponent<DistributedLoopie>().SetViewpointPosition(viewpointHandPosition);
                        });
                },
                (_, recordingModel) => recordingModel.HeldLoopie.GetComponent<DistributedLoopie>().FinishRecording());

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
                        pplusModel,
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
                        pplusModel,
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

            #region Apply menu

            // Shared state machine storage for the level widget that visualizes changing the level.
            // NO NO NO DO NOT DO THIS IT GETS SHARED ACROSS ALL INSTANCES OF THE STATE MACHINE NOOOOOOOO
            // (it works only when there is exactly one state machine instance in existence)
            // DistributedLevelWidget widget = null;

            State<PPlusEvent, MenuVerbModel, PPlusModel> applyMenuVerb = new State<PPlusEvent, MenuVerbModel, PPlusModel>(
                "levelChange",
                initial,
                (evt, pplusModel) =>
                {
                    DistributedLevelWidget widget = pplusModel.Controller.CreateLevelWidget().GetComponent<DistributedLevelWidget>();
                    float initialHandYPosition = pplusModel.Controller.GetViewpointHandPosition().y;

                    MenuVerb menuVerb = pplusModel.Controller.CurrentlyHeldVerb;
                    HoloDebug.Log($"Entering levelChange state, menuVerb is {menuVerb.Name} of kind {menuVerb.Kind}");

                    if (menuVerb.Kind == MenuVerbKind.Prompt)
                    {
                        // take effect right now!
                        // Prompt menu verbs just execute.
                        menuVerb.PromptAction();
                    }

                    return new MenuVerbModel(
                        pplusModel,
                        menuVerbModel =>
                        {
                            if (menuVerb.Kind == MenuVerbKind.Prompt)
                            {
                                // we already happened; update does nothing
                                return;
                            }

                            // If this is a Touch menu verb, then update the touched loopie set.
                            if (menuVerb.Kind == MenuVerbKind.Touch)
                            {
                                pplusModel.Controller.UpdateTouchedLoopieList();

                                DistributedPerformer performer = pplusModel.Controller.DistributedPerformer;
                                HashSet<DistributedId> ids = new HashSet<DistributedId>(
                                    ((LocalPerformer)performer.LocalObject)
                                        .GetState()
                                        .TouchedLoopieIdList
                                        .Select(id => new DistributedId(id)));
                                menuVerb.TouchAction(ids);
                            }
                            else
                            {
                                float lastAdjustment = menuVerbModel.Adjustment;

                                float currentHandYPosition = pplusModel.Controller.GetViewpointHandPosition().y;

                                float adjustment = (currentHandYPosition - initialHandYPosition) / MagicNumbers.MaxVolumeHeightMeters;
                                // clamp this to (-1, 1) interval
                                adjustment = Math.Min(1f, Math.Max(-1f, adjustment));

                                HoloDebug.Log($"ControllerStateMachineInstance.LouderSofter: initialHandY {initialHandYPosition}, currentHandY {currentHandYPosition}, adjustment {adjustment}, lastAdjustment {lastAdjustment}");

                                Core.Contract.Assert(!float.IsNaN(adjustment));
                                Core.Contract.Assert(!float.IsInfinity(adjustment));

                                menuVerbModel.Adjustment = adjustment;

                                LevelWidgetState state = widget.State;
                                widget.UpdateState(
                                    new LevelWidgetState { ViewpointPosition = state.ViewpointPosition, Adjustment = adjustment });

                                ApplyLevelVerb(pplusModel, menuVerb, adjustment, false);
                            }
                        },
                        widget);
                },
                (evt, levelAdjustModel) =>
                {
                    PPlusModel pplusModel = (PPlusModel)levelAdjustModel.Parent;
                    ApplyLevelVerb(pplusModel, pplusModel.Controller.CurrentlyHeldVerb, levelAdjustModel.Adjustment, true);

                    if (levelAdjustModel.LevelWidget != null)
                    {
                        levelAdjustModel.LevelWidget.Delete();
                    }
                });

            // Light button will do something if there is a current menu verb.
            AddTransition(
                stateMachine,
                initial,
                PPlusEvent.LightDown,
                applyMenuVerb,
                pplusModel => pplusModel.Controller.CurrentlyHeldVerb.IsDefined);
            AddTransition(stateMachine, applyMenuVerb, PPlusEvent.LightUp, initial);

            #endregion

            #region Popup menu

            var menu = new State<PPlusEvent, MenuModel, PPlusModel>(
                "Menu",
                initial,
                (evt, pplusModel) => new MenuModel(
                    pplusModel,
                    _ => { }, // nothing to do when updating; the MenuController gets its own Update call
                    pplusModel.Controller.CreateMenu().GetComponent<DistributedMenu>()),
                (evt, menuModel) => {
                    DistributedMenu menu = menuModel.Menu;
                    MenuVerb heldVerb = menu.GetMenuVerb();
                    ((PPlusModel)menuModel.Parent).Controller.CurrentlyHeldVerb = heldVerb;

                    HoloDebug.Log($"Set currentlyHeldVerb to {heldVerb.Name}");
                    HoloDebug.Log($"ControllerStateMachineInstance.Menu.exit: deleting menu {menu.Id}");
                    menu.Delete();
                });

            AddTransition(stateMachine, initial, PPlusEvent.TeamsDown, menu);
            AddTransition(stateMachine, menu, PPlusEvent.TeamsUp, initial);

            #endregion

            return stateMachine;
        }

        /// <summary>
        /// Apply this MenuVerb with the given adjustment and perhaps commit.
        /// </summary>
        private static void ApplyLevelVerb(PPlusModel pplusModel, MenuVerb menuVerb, float adjustment, bool commit)
        {
            // Now apply the adjustment appropriately.
            // Is the microphone hand close to the performer's mouth?
            // First, which player ID are we?
            int playerIndex = pplusModel.Controller.playerIndex;
            PlayerState playerState;
            bool mikeNextToMouth = false;
            // Now, which Player is that?
            if (DistributedViewpoint.Instance != null
                && DistributedViewpoint.Instance.TryGetPlayerById((PlayerId)playerIndex, out playerState))
            {
                // get the distance between the player's non-controller hand and head
                Vector3 playerHeadPos = playerState.HeadPosition;
                Side handSide = pplusModel.Controller.handSide;
                Vector3 mikeHandPos = handSide == Side.Left ? playerState.RightHandPosition : playerState.LeftHandPosition;

                mikeNextToMouth = Vector3.Distance(playerHeadPos, mikeHandPos) < MagicNumbers.MaximumHeadToMikeHandDistance;
            }

            HashSet<DistributedId> ids;
            DistributedPerformer performer = pplusModel.Controller.DistributedPerformer;
            if (mikeNextToMouth && menuVerb.MayBePerformer)
            {
                ids = new HashSet<DistributedId>(new[] { performer.Id });
            }
            else
            {
                ids = new HashSet<DistributedId>(
                    ((LocalPerformer)performer.LocalObject)
                        .GetState()
                        .TouchedLoopieIdList
                        .Select(id => new DistributedId(id)));
            }

            menuVerb.LevelAction(ids, adjustment, commit);
        }
    }
}
