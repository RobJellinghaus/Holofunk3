/// Copyright by Rob Jellinghaus.  All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Loop;
using Holofunk.Perform;
using Holofunk.StateMachines;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using NowSoundLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Whhat should this hand do when it touches a loopie?
        /// </summary>
        /// <remarks>
        /// This will be applied repeatedly on every update in which a loopie is touched, so idempotency is strongly recommended!
        /// </remarks>
        internal Action<DistributedLoopie> touchedLoopieAction;

        /// <summary>
        /// The sorted IDs of the loopies touched by this hand.
        /// </summary>
        /// <remarks>
        /// Recalculated anew on every frame.
        /// </remarks>
        private readonly List<DistributedId> touchedLoopieIds = new List<DistributedId>();

        /// <summary>
        /// The previous contents of touchedLoopies (for debugging).
        /// </summary>
        private readonly List<DistributedId> previouslyTouchedLoopieIds = new List<DistributedId>();

        /// <summary>
        /// The (sorted) IDs of the loopies currently touched by this hand.
        /// </summary>
        public IEnumerable<DistributedId> TouchedLoopieIds => touchedLoopieIds;

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

        /// <summary>
        /// Any loopies touched by this hand?
        /// </summary>
        internal bool AnyLoopiesTouched => touchedLoopieIds.Count > 0;

        internal void ApplyToTouchedLoopies(Action<DistributedLoopie> action)
        {
            if (action != null)
            {
                foreach (LocalLoopie localLoopie in
                    DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                        DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
                {
                    if (touchedLoopieIds.Contains(localLoopie.DistributedObject.Id))
                    {
                        action((DistributedLoopie)localLoopie.DistributedObject);
                    }
                }
            }
        }

        /// <summary>
        /// Get the PerformerController component from our parent..
        /// </summary>
        internal PerformerPreController PerformerController => gameObject.transform.parent.gameObject.GetComponent<PerformerPreController>();

        /// <summary>
        /// Get the DistributedPerformer component from our parent.
        /// </summary>
        internal DistributedPerformer DistributedPerformer => gameObject.transform.parent.gameObject.GetComponent<DistributedPerformer>();

        /// <summary>
        /// for debugging only
        /// </summary>
        internal string HandStateMachineInstanceString => stateMachineInstance?.ToString() ?? "";

        // Update is called once per frame
        void Update()
        {
            PerformerState performer = DistributedPerformer.GetPerformer();

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

            // Update the loopie's position while the user is holding it.
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

            // Update touched loopies first, before updating hand position and state.
            if (KeepTouchedLoopiesStable)
            {
                // do nothing! the previous TouchedLoopieIdList can remain the same.
                // Even if some or all of the loopies in it have been deleted, the effect
                // of this is benign because when we go to actually traverse them, we
                // will skip them.
            }
            else
            {
                // freely update the touched loopie list of the performer
                UpdateTouchedLoopies(ref performer);
            }

            ApplyToTouchedLoopies(touchedLoopieAction);
        }

        /// <summary>
        /// Update the local lists of loopies touched by this hand.
        /// </summary>
        private void UpdateTouchedLoopies(ref PerformerState performer)
        {
            touchedLoopieIds.Clear();

            Vector3 handPosition = HandPosition(ref performer);

            foreach (LocalLoopie localLoopie in
                DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                    DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
            {
                Vector3 loopiePosition = localLoopie.transform.position;
                if (Vector3.Distance(loopiePosition, handPosition) < MagicNumbers.HandRadius)
                {
                    touchedLoopieIds.Add(localLoopie.DistributedObject.Id);
                }
            }

            touchedLoopieIds.Sort(DistributedId.Comparer.Instance);

            if (!EqualLists(previouslyTouchedLoopieIds, touchedLoopieIds))
            {
                previouslyTouchedLoopieIds.Clear();
                previouslyTouchedLoopieIds.AddRange(touchedLoopieIds);
                HoloDebug.Log($"Updating {handSide} touched loopies: prior [{string.Join(", ", previouslyTouchedLoopieIds)}], current [{string.Join(", ", touchedLoopieIds)}]");
            }
        }

        private bool EqualLists(List<DistributedId> list1, List<DistributedId> list2)
        {
            if (list1.Count != list2.Count)
            {
                return false;
            }
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i] != list2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public HandPoseValue HandPose(ref PerformerState performer) => handSide == Side.Left 
            ? performer.LeftHandPose 
            : performer.RightHandPose;

        public Vector3 HandPosition(ref PerformerState performer) => handSide == Side.Left 
            ? performer.LeftHandPosition 
            : performer.RightHandPosition;

        /// <summary>
        /// Update the hand state, and if appropriate, create an event and pass it to the state machine.
        /// Note that BodyRelativeHandPosition may override currentHandState.
        /// </summary>
        /// <param name="handPose">Current (smoothed) hand pose from the performer</param>
        /// <param name="performer">The actual performer, passed by ref for efficiency (no mutation please)</param>
        private void UpdateHandState(HandPoseValue handPose, ref PerformerState performer)
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
                    stateMachineInstance.OnNext(handPoseEvent);
                }
            }
            else
            {
                if (lastHandPose.Value != handPose)
                {
                    lastHandPose = handPose;

                    stateMachineInstance.OnNext(handPoseEvent);
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

            PerformerState performer = DistributedPerformer.GetPerformer();

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
    }
}
