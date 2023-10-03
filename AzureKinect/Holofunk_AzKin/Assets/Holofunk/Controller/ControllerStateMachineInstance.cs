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
    using ControllerState = State<PPlusEvent, PPlusController, PPlusController>;
    using ControllerAction = Action<PPlusEvent, PPlusController>;
 
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
        static void AddTransition<TModel>(ControllerStateMachine ret, State<PPlusEvent, TModel> from, PPlusEvent evt, State<PPlusEvent> to, Func<bool> guardFunc)
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

        // Set up the state machine we want for our dear little Loopies.
        static ControllerStateMachine MakeControllerStateMachine()
        {
            ThreadContract.RequireUnity();

            ControllerState root = new ControllerState("root", null, new ControllerAction[0], new ControllerAction[0]);

            // Base state: just playing along.
            var initial = new ControllerState(
                "initial",
                root,
                (evt, pplusController) => { },
                (evt, pplusController) => { });

            var stateMachine = new ControllerStateMachine(initial, PPlusEventComparer.Instance);

            #region Recording

            // We're recording.
            ControllerState recording = new ControllerState(
                "recording",
                initial,
                (evt, pplusController) =>
                {
                    //pplusController.PushSprite(SpriteId.HollowCircle, Color.red);

                    // Creating the loopie here assigns it as the currently held loopie.
                    // Note that this implicitly starts recording.
                    pplusController.CreateLoopie(
                        pplusController.playerIndex == 0 
                            ? NowSoundLib.AudioInputId.AudioInput1
                            : NowSoundLib.AudioInputId.AudioInput2);
                },
                (evt, pplusController) =>
                {
                    //pplusController.PopGameObject();
                    pplusController.ReleaseLoopie();
                });

            AddTransition(
                stateMachine,
                initial,
                PPlusEvent.MikeDown,
                // Start recording if and only if 1) the UI didn't capture this, and 2) recording is enabled.
                (evt, pplusController) => (!evt.IsCaptured /* && HolofunkController.Instance.IsRecordingEnabled */) ? recording : initial);

            AddTransition(stateMachine, recording, PPlusEvent.MikeUp, initial);

            #endregion

            #region Mute/unmute

            ControllerState mute = new ControllerState(
                "mute",
                initial,
                (evt, pplusController) =>
                {
                    // initialize whether we are deleting the loopies we touch
                    Option<bool> deletingTouchedLoopies = Option<bool>.None;

                    // Collection of loopies that makes sure we don't flip loopies back and forth between states.
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    pplusController.SetTouchedLoopieAction(loopie =>
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
                (evt, pplusController) => pplusController.SetTouchedLoopieAction(null));

            AddTransition(stateMachine, initial, PPlusEvent.LeftDown, mute);
            AddTransition(stateMachine, mute, PPlusEvent.LeftUp, initial);

            ControllerState unmute = new ControllerState(
                "unmute",
                initial,
                (evt, pplusController) =>
                {
                    HashSet<DistributedId> toggledLoopies = new HashSet<DistributedId>();

                    pplusController.SetTouchedLoopieAction(loopie =>
                    {
                        if (!toggledLoopies.Contains(loopie.Id))
                        {
                            toggledLoopies.Add(loopie.Id); // loopity doo, I've got another puzzle for you
                            loopie.SetMute(false);
                        }
                    });
                },
                (evt, pplusController) => pplusController.SetTouchedLoopieAction(null));

            AddTransition(stateMachine, initial, PPlusEvent.RightDown, unmute);
            AddTransition(stateMachine, unmute, PPlusEvent.RightUp, initial);

            #endregion

            #region Louden/soften

            // Shared state machine storage for the volume widget that visualizes changing the volume.
            DistributedVolumeWidget widget = null;

            // we're pointing, and about to possibly mute/unmute
            ControllerState loudenSoften = new ControllerState(
                "loudenSoften",
                initial,
                (evt, pplusController) =>
                {
                    // keep the set of touched loopies stable, so whatever we originally touched is still what we louden/soften
                    pplusController.KeepTouchedLoopiesStable = true;

                    widget = pplusController.CreateVolumeWidget().GetComponent<DistributedVolumeWidget>();
                    float initialHandYPosition = pplusController.GetViewpointHandPosition().y;
                    float lastVolumeRatio = 1;
                    float volumeRatio = 1;

                    pplusController.SetUpdateAction(() =>
                    {
                        lastVolumeRatio = volumeRatio;

                        float currentHandYPosition = pplusController.GetViewpointHandPosition().y;

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

                    pplusController.SetTouchedLoopieAction(loopie =>
                    {
                        if (lastVolumeRatio != volumeRatio)
                        {
                            loopie.MultiplyVolume(volumeRatio / lastVolumeRatio);
                        }
                    });
                },
                (evt, pplusController) =>
                {
                    // technically this should revert to whatever it was before, but we know in this case this was false before
                    pplusController.KeepTouchedLoopiesStable = false;

                    pplusController.SetUpdateAction(null);
                    pplusController.SetTouchedLoopieAction(null);

                    widget.Delete();
                });
            /*
            AddTransition(stateMachine, initial, PPlusEvent.ShoulderPressed, loudenSoften);
            AddTransition(stateMachine, loudenSoften, PPlusEvent.ShoulderReleased, initial);
            */

            #endregion

            #region Effect popup menus

            // State machine construction ensures there will only be one of these per hand state machine instance,
            // so we can safely use a local variable here to close over this menu
            var soundEffectMenu = CreateMenuState(initial, MenuKinds.SoundEffects);

            AddTransition(stateMachine, initial, PPlusEvent.LightDown, soundEffectMenu);
            AddTransition(stateMachine, soundEffectMenu, PPlusEvent.LightUp, initial);

            #endregion

            #region System popup menu

            ControllerState systemMenu = CreateMenuState(initial, MenuKinds.System);

            AddTransition(stateMachine, initial, PPlusEvent.TeamsDown, systemMenu);
            AddTransition(stateMachine, systemMenu, PPlusEvent.TeamsUp, initial);

            #endregion

            return stateMachine;
        }

        private static ControllerState CreateMenuState(ControllerState initial, MenuKinds menuKind)
        {
            // State machine construction ensures there will only be one of these per state machine instance.
            // But see bug below... there may be some incorrectness here....
            GameObject menuGameObject = null;

            var menu = new ControllerState(
                menuKind.ToString(),
                initial,
                (evt, pplusController) =>
                {
                    // keep the set of touched loopies stable, so whatever we originally touched is still what we apply sound effects to
                    pplusController.KeepTouchedLoopiesStable = true;

                    menuGameObject = CreateMenu(pplusController, menuKind);
                },
                (evt, pplusController) => {
                    // got one crash on the "DistributeMenu menu = menuGameObject.GetComponent<...>(...)" line below.
                    // Speculation is that there could be multiple events in flight, or something, such that this can
                    // incorrectly be reached more than once on the way out of the state machine.
                    // TODO: verify what could go wrong here, this fix may or may not have unintended consequences.
                    if (menuGameObject != null)
                    {
                        // let loopies get (un)touched again
                        pplusController.KeepTouchedLoopiesStable = false;

                        HashSet<DistributedId> touchedLoopies = new HashSet<DistributedId>(pplusController.TouchedLoopieIds);

                        DistributedMenu menu = menuGameObject.GetComponent<DistributedMenu>();
                        HoloDebug.Log($"ControllerStateMachineInstance.systemMenu.exit: calling menu action on {touchedLoopies.Count} loopies");
                        menu.InvokeSelectedAction(touchedLoopies);

                        // delete it in the distributed sense.
                        // note that locally, this will synchronously destroy the game object
                        HoloDebug.Log($"ControllerStateMachineInstance.systemMenu.exit: deleting menu {menu.Id}");
                        menu.Delete();
                        menuGameObject = null;
                    }
                });

            return menu;
        }

        private static GameObject CreateMenu(PPlusController pplusController, MenuKinds menuKind)
        {
            HoloDebug.Log($"Creating menu kind {menuKind} for pplusController #{pplusController.playerIndex}{pplusController.handSide}");

            // get the forward direction towards the camera from the hand location
            Vector3 localHandPosition = pplusController.GetViewpointHandPosition();

            Vector3 viewpointHandPosition = localHandPosition;
            // was previously: DistributedViewpoint.Instance.LocalToViewpointMatrix().MultiplyPoint(localHandPosition);

            Vector3 viewpointForwardDirection = Vector3.forward;

            GameObject menuGameObject = DistributedMenu.Create(
                menuKind,
                viewpointForwardDirection,
                viewpointHandPosition);

            menuGameObject.GetComponent<MenuController>().Initialize(pplusController);

            return menuGameObject;
        }
    }
}
