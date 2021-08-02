/// Copyright by Rob Jellinghaus.  All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Loopie;
using Holofunk.Perform;
using Holofunk.StateMachines;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using NowSoundLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.HandComponents
{
    using HandState = State<HandPoseEvent, HandController, HandController>;
    using HandAction = Action<HandPoseEvent, HandController>;
    //using HandToHandMenuState = State<HandPoseEvent, MenuModel<HandController>, HandController>;
    using HandStateMachineInstance = StateMachineInstance<HandPoseEvent>;

    /// <summary>
    /// Tracks the state and interactions of one of the performer's hands.
    /// </summary>
    public class HandController : MonoBehaviour, IModel
    {
        [Tooltip("Which hand.")]
        public Side handSide = Side.Left;

        /// <summary>
        /// The last hand state of this hand (after the top-counting).
        /// </summary>
        /// <remarks>
        /// When first initialized, has no value.
        /// </remarks>
        private Option<HandPoseValue> lastHandPose;

        /// <summary>
        /// The state machine for handling the hand state.
        /// </summary>
        private HandStateMachineInstance stateMachineInstance;

        /// <summary>
        /// The loopie currently being held by the player.
        /// </summary>
        /// <remarks>
        /// This is only ever non-null when the handStateMachineInstance is in recording state.
        /// </remarks>
        private GameObject currentlyHeldLoopie;

        /// <summary>
        /// The radius of the hand in world space.
        /// </summary>
        /// <remarks>
        /// Determined from the X scale of the HollowSprite circle.
        /// </remarks>
        private float handRadius = 0.1f; // 10 cm = 4 inches. Pretty big but let's start there

        /// <summary>
        /// Whhat should this hand do when it touches a loopie?
        /// </summary>
        /// <remarks>
        /// This will be applied repeatedly on every update in which a loopie is touched, so idempotency is strongly recommended!
        /// </remarks>
        internal Action<DistributedLoopie> touchedLoopieAction;

        /// <summary>
        /// The loopies touched by this hand.
        /// </summary>
        private readonly List<DistributedLoopie> touchedLoopies = new List<DistributedLoopie>();

        /// <summary>
        /// Debugging touched loopies issues is impossible unless you log only the *changes* in the touched loopie list.
        /// </summary>
        private readonly List<DistributedLoopie> previouslyTouchedLoopies = new List<DistributedLoopie>();

        /// <summary>
        /// Should hand position be ignored when determining hand pose?
        /// </summary>
        /// <remarks>
        /// This is enabled by popup menus when they come up, to prevent hand position changes from kicking you out of the menu.
        /// </remarks>
        internal bool IgnoreHandPositionForHandPose { get; set; }

        /// <summary>
        /// Should the set of touched loopies be kept stable, rather than recomputed on each update?
        /// </summary>
        /// <remarks>
        /// When using a popup menu created by touching some loopie(s), we do not want to un-touch the loopies while picking from
        /// the menu. Setting this flag causes the set of touched loopies to be stabilized for the duration of the flag.
        /// </remarks>
        internal bool KeepTouchedLoopiesStable { get; set; }

        internal bool AnyLoopiesTouched => touchedLoopies.Count > 0;

        internal void ApplyToTouchedLoopies(Action<DistributedLoopie> action)
        {
            touchedLoopies.ForEach(action);
        }

        /// <summary>
        /// Get the parent PerformerController.
        /// </summary>
        internal PerformerController PerformerController => gameObject.transform.parent.gameObject.GetComponent<PerformerController>();

        internal DistributedPerformer DistributedPerformer => gameObject.transform.parent.gameObject.GetComponent<DistributedPerformer>();

        /// <summary>
        /// for debugging only
        /// </summary>
        internal string HandStateMachineInstanceString => stateMachineInstance?.ToString() ?? "";

        // Update is called once per frame
        void Update()
        {
            /* prior code for loopie selection... needs updates now.
            // Update touched loopies first, before updating hand position and state.
            if (KeepTouchedLoopiesStable)
            {
                // visually ensure the loopies still look touched
                ApplyToTouchedLoopies(loopie => loopie.AppearTouched());
            }
            else
            {
                // freely update the touched loopie set
                UpdateTouchedLoopies();
            }
            */

            Performer performer = DistributedPerformer.GetPerformer();

            if (PerformerController.OurPlayer.PerformerHostAddress == default(SerializedSocketAddress))
            {
                // if we have a state machine, shut it down now.
                if (stateMachineInstance != null)
                {
                    stateMachineInstance.OnCompleted();
                    stateMachineInstance = null;
                }

                // we aren't recognized yet... state machine not running.
                return;
            }

            // if we don't have a state machine instance yet, then we should now create one!
            // (it exists only as long as we are recognized by the viewpoint.)
            if (stateMachineInstance == null)
            {
                stateMachineInstance = new HandStateMachineInstance(HandPoseEvent.Opened, HandStateMachine.Instance, this);
            }

            // Update the hand state, based on the latest known Kinect hand state.
            HandPoseValue currentHandPose = HandPose(ref performer);

            // Pass the performer state down by ref for efficiency (no mutation please, it'll be lost)
            UpdateHandState(currentHandPose, ref performer);

            if (currentlyHeldLoopie != null && DistributedViewpoint.TheViewpoint != null)
            {
                Vector3 performerHandPosition = HandPosition(ref performer);
                Matrix4x4 localToViewpointMatrix = DistributedViewpoint.TheViewpoint.LocalToViewpointMatrix();
                if (localToViewpointMatrix != Matrix4x4.zero)
                {
                    Vector3 viewpointHandPosition = localToViewpointMatrix.MultiplyPoint(performerHandPosition);
                    currentlyHeldLoopie.GetComponent<DistributedLoopie>().SetViewpointPosition(viewpointHandPosition);
                }
            }
        }

        private HandPoseValue HandPose(ref Performer performer) => handSide == Side.Left 
            ? performer.LeftHandPose 
            : performer.RightHandPose;

        private Vector3 HandPosition(ref Performer performer) => handSide == Side.Left 
            ? performer.LeftHandPosition 
            : performer.RightHandPosition;

        /// <summary>
        /// Update the hand state, and if appropriate, create an event and pass it to the state machine.
        /// Note that BodyRelativeHandPosition may override currentHandState.
        /// </summary>
        /// <param name="handPose">Current (smoothed) hand pose from the performer</param>
        /// <param name="performer">The actual performer, passed by ref for efficiency (no mutation please)</param>
        private void UpdateHandState(HandPoseValue handPose, ref Performer performer)
        {
            HandPoseEvent handPoseEvent = HandPoseEvent.FromHandPose(handPose);

            if (!lastHandPose.HasValue)
            {
                // This is the very first hand state ever.
                lastHandPose = handPose;

                // Create state machine instance.
                stateMachineInstance = new HandStateMachineInstance(HandPoseEvent.Unknown, HandStateMachine.Instance, this);
                
                // TODO: do we do an immediate OnNext? If not, then shouldn't we be setting lastHandPose to Unknown here?
                if (handPose != HandPoseValue.Unknown)
                {
                    stateMachineInstance.OnNext(handPoseEvent, default(Moment));
                }
            }
            else
            {
                if (lastHandPose.Value != handPose)
                {
                    lastHandPose = handPose;

                    stateMachineInstance.OnNext(handPoseEvent, default(Moment));
                }
            }
        }

        /// <summary>
        /// Create a new loopie at the current hand's position, and set it as the currentlyHeldLoopie.
        /// </summary>
        public void CreateLoopie()
        {
            Holofunk.Core.Contract.Requires(currentlyHeldLoopie == null);

            if (DistributedViewpoint.TheViewpoint == null)
            {
                HoloDebug.Log("No DistributedViewpoint.TheViewpoint; can't create loopie");
                return;
            }

            Performer performer = DistributedPerformer.GetPerformer();

            // performer space hand position
            Vector3 performerHandPosition = HandPosition(ref performer);
            Matrix4x4 localToViewpointMatrix = DistributedViewpoint.TheViewpoint.LocalToViewpointMatrix();
            Vector3 viewpointHandPosition = localToViewpointMatrix.MultiplyPoint(performerHandPosition);

            GameObject newLoopie = DistributedLoopie.Create(viewpointHandPosition);
            currentlyHeldLoopie = newLoopie;
        }

        /// <summary>
        /// Let go of the currentlyHeldLoopie, leaving it at its world space position.
        /// </summary>
        public void ReleaseLoopie()
        {
            if (currentlyHeldLoopie == null)
            {
                //Debug.Log("HandController.ReleaseLoopie(): currentlyHeldLoopie is null and should not be!");
            }
            else
            {
                currentlyHeldLoopie.GetComponent<DistributedLoopie>().FinishRecording();

                currentlyHeldLoopie = null;
            }
        }

        /* save for later once we actually have some
        private void UpdateTouchedLoopies()
        {
            previouslyTouchedLoopies.Clear();
            previouslyTouchedLoopies.AddRange(touchedLoopies);

            GameObject mainCamera = null;
            Vector3 cameraToHand = Vector3.zero;
            if (!MagicConstants.UseDistanceBasedTouching)
            {
                mainCamera = GameObject.Find("MainCamera");
                cameraToHand = (transform.position - mainCamera.transform.position).normalized;
            }

            touchedLoopies.Clear();

            DistributedLoopie.Apply(loopie =>
            {
                if (MagicConstants.UseDistanceBasedTouching)
                {
                    Vector3 distanceVector = loopie.transform.position - transform.position;
                    float distance = distanceVector.magnitude;
                    //builder.AppendFormat("[{0} dist {1}", i, distance);
                    if (distance < handRadius)
                    {
                        touchedLoopies.Add(loopie);
                        loopie.AppearTouched();

                        if (touchedLoopieAction != null)
                        {
                            touchedLoopieAction(loopie);
                        }
                    }
                }
                else
                {
                    Vector3 cameraToLoopie = (loopie.transform.position - mainCamera.transform.position).normalized;
                    float dotProduct = Vector3.Dot(cameraToHand, cameraToLoopie);

                    if (dotProduct > MagicConstants.MinimumDotProductForRayBasedTouching)
                    {
                        touchedLoopies.Add(loopie);
                        loopie.AppearTouched();

                        if (touchedLoopieAction != null)
                        {
                            touchedLoopieAction(loopie);
                        }
                    }
                }
            });

            if (previouslyTouchedLoopies.Count != touchedLoopies.Count)
            {
                Debug.Log($"HandController.UpdateTouchedLoopies(): player {playerController.playerIndex} {handSide} hand: now touching [{string.Join(", ", touchedLoopies.Select(loopie => loopie.TrackId.ToString()))}]");
            }
        }
        */
    }
}
