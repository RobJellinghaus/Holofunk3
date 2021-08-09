/// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Hand;
using Holofunk.Loop;
using Holofunk.StateMachines;
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

            return stateMachine;
        }
    }
}