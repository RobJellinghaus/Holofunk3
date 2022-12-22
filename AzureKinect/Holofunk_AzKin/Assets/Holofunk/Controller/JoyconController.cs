/// Copyright by Rob Jellinghaus.  All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Loop;
using Holofunk.Perform;
using Holofunk.Sound;
using Holofunk.StateMachines;
using Holofunk.Viewpoint;
using Holofunk.VolumeWidget;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Controller
{
    using ControllerStateMachineInstance = StateMachineInstance<JoyconEvent>;

    /// <summary>
    /// Tracks the state and interactions of a single player's controller.
    /// </summary>
    /// <remarks>
    /// Arguably this is a horrible amount of code duplication vs. the HandController in the HoloLens 2 code
    /// base, but pending some actual revival of the mixed reality version, not worrying about that right now.
    /// </remarks>
    public class JoyconController : MonoBehaviour, IModel
    {
        /// <summary>
        /// Index of the JoyCon selected by this Controller; -1 means "unknown".
        /// </summary>
        /// <remarks>
        /// This is the index into the JoyconManager.Instance.j array. (Yes, JoyconManager
        /// uses public instance variables... oh well)
        /// </remarks>
        [Tooltip("Joycon index in JoyconManager")]
        public int joyconIndex = 0;

        /// <summary>
        /// The player ID of this player, in the current viewpoint's view.
        /// </summary>
        [Tooltip("Player index in current viewpoint")]
        public int playerIndex;

        [Tooltip("Which hand.")]
        public Side handSide = Side.Left;

        /// <summary>
        /// The state machine for handling the player's state.
        /// </summary>
        private ControllerStateMachineInstance stateMachineInstance;

        /// <summary>
        /// The loopie currently being held by this controller.
        /// </summary>
        /// <remarks>
        /// This is only ever non-null when the stateMachineInstance is in recording state.
        /// </remarks>
        private GameObject currentlyHeldLoopie;

        /// <summary>
        /// What should this controller do when it touches a loopie?
        /// </summary>
        /// <remarks>
        /// This will be applied repeatedly on every update in which a loopie is touched, so idempotency is strongly recommended!
        /// </remarks>
        private Action<DistributedLoopie> touchedLoopieAction;

        /// <summary>
        /// What should this controller do on every update, before touching loopies?
        /// </summary>
        /// <remarks>
        /// This is used for, e.g., updating the current volume widget and performing other interactions that aren't
        /// loopie-centric.
        /// TODO: could touchedLoopieAction just be a sort of updateAction?
        /// </remarks>
        private Action updateAction;

        /// <summary>
        /// The sorted IDs of the loopies touched by this controller.
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
        /// The (sorted) IDs of the loopies currently touched by this controller.
        /// </summary>
        public IEnumerable<DistributedId> TouchedLoopieIds => touchedLoopieIds;

        /// <summary>
        /// Should the set of touched loopies be kept stable, rather than recomputed on each update?
        /// </summary>
        /// <remarks>
        /// When using a popup menu created by touching some loopie(s), we do not want to un-touch the loopies while picking from
        /// the menu. Setting this flag causes the set of touched loopies to be stabilized for the duration of the flag.
        /// There is probably a better way to think about this, but pending a fuller reactive or other UI structure,
        /// we'll live with this.
        /// </remarks>
        internal bool KeepTouchedLoopiesStable { get; set; }

        /// <summary>
        /// Any loopies touched by this controller?
        /// </summary>
        internal bool AnyLoopiesTouched => touchedLoopieIds.Count > 0;

        internal void SetUpdateAction(Action updateAction)
            => this.updateAction = updateAction;

        internal void SetTouchedLoopieAction(Action<DistributedLoopie> touchedLoopieAction)
            => this.touchedLoopieAction = touchedLoopieAction;

        private void ApplyToTouchedLoopies(Action<DistributedLoopie> action)
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

        /*
        /// <summary>
        /// Get the PerformerController component from our parent..
        /// </summary>
        internal PerformerPreController PerformerController => gameObject.transform.parent.gameObject.GetComponent<PerformerPreController>();
        */

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
            if (joyconIndex == -1 || JoyconManager.Instance == null || JoyconManager.Instance.j.Count <= joyconIndex)
            {
                // No joyCon yet associated with this player; nothing to do.
                return;
            }

            if (DistributedViewpoint.Instance == null || DistributedViewpoint.Instance.PlayerCount <= playerIndex)
            {
                // No Kinect player recognized yet.
                return;
            }

            Joycon thisJoycon = JoyconManager.Instance.j[joyconIndex];

            // Joycon bailed, we're done
            if (thisJoycon.state == Joycon.state_.DROPPED)
            {
                if (stateMachineInstance != null)
                {
                    stateMachineInstance.OnCompleted();
                    stateMachineInstance = null;
                }
                return;
            }

            // if we don't have a state machine instance yet, then we should now create one!
            // (it exists only as long as we are recognized by the viewpoint.)
            if (stateMachineInstance == null)
            {
                stateMachineInstance = new ControllerStateMachineInstance(JoyconEvent.TriggerReleased, ControllerStateMachine.Instance, this);
            }

            // Create any pressed/released events as appropriate; we don't track each shoulder/trigger button
            // separately.
            CheckButtons(thisJoycon);

            // Update the loopie's position while the user is holding it.
            if (currentlyHeldLoopie != null)
            {
                Vector3 viewpointHandPosition = GetViewpointHandPosition();
                /*
                Matrix4x4 localToViewpointMatrix = DistributedViewpoint.Instance.LocalToViewpointMatrix();
                if (localToViewpointMatrix != Matrix4x4.zero)
                {
                    Vector3 viewpointHandPosition = localToViewpointMatrix.MultiplyPoint(performerHandPosition);
                }
                */
                HoloDebug.Log($"Updated viewport hand position of loopie {currentlyHeldLoopie} to {viewpointHandPosition}");
                currentlyHeldLoopie.GetComponent<DistributedLoopie>().SetViewpointPosition(viewpointHandPosition);
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
                UpdateTouchedLoopieList();
            }

            // apply the update action, if any
            if (updateAction != null)
            {
                updateAction();
            }

            ApplyToTouchedLoopies(touchedLoopieAction);
        }

        /// <summary>
        /// Fire down/up events for any pressed/released buttons in the past interval.
        /// </summary>
        /// <param name="joycon"></param>
        private void CheckButtons(Joycon joycon)
        {
            for (Joycon.Button b = Joycon.Button.DPAD_DOWN; b <= Joycon.Button.SHOULDER_2; b++)
            {
                if (joycon.GetButtonDown(b))
                {
                    HoloDebug.Log($"Joycon button down: {b}: posting down event");
                    stateMachineInstance.OnNext(new JoyconEvent(b, isDown: true));
                }
                if (joycon.GetButtonUp(b))
                {
                    HoloDebug.Log($"Joycon button up: {b}: posting up event");
                    stateMachineInstance.OnNext(new JoyconEvent(b, isDown: false));
                }
            }
        }

        /// <summary>
        /// Update the local lists of loopies touched by this hand.
        /// </summary>
        private void UpdateTouchedLoopieList()
        {
            touchedLoopieIds.Clear();

            Vector3 handPosition = GetViewpointHandPosition();

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

            // logging changes in touched loopie list
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

        /// <summary>
        /// Create a new loopie at the current hand's position, and set it as the currentlyHeldLoopie.
        /// </summary>
        public void CreateLoopie()
        {
            Holofunk.Core.Contract.Requires(currentlyHeldLoopie == null);

            if (DistributedViewpoint.Instance == null)
            {
                HoloDebug.Log("No DistributedViewpoint.TheViewpoint; can't create loopie");
                return;
            }

            Vector3 viewpointHandPosition = GetViewpointHandPosition();

            GameObject newLoopie = DistributedLoopie.Create(viewpointHandPosition);
            currentlyHeldLoopie = newLoopie;
        }

        /// <summary>
        /// Create a volume widget.
        /// </summary>
        public GameObject CreateVolumeWidget()
        {
            if (DistributedViewpoint.Instance == null)
            {
                HoloDebug.Log("No DistributedViewpoint.TheViewpoint; can't create volume widget");
                return null;
            }

            Vector3 viewpointHandPosition = GetViewpointHandPosition();

            GameObject newWidget = DistributedVolumeWidget.Create(viewpointHandPosition);
            return newWidget;
        }

        public Vector3 GetViewpointHeadPosition()
        {
            // hand of this player
            PlayerState thisPlayer = DistributedViewpoint.Instance.GetPlayerByIndex(playerIndex);
            return thisPlayer.HeadPosition;
        }

        public Vector3 GetViewpointHandPosition()
        {
            // hand of this player
            PlayerState thisPlayer = DistributedViewpoint.Instance.GetPlayerByIndex(playerIndex);
            Vector3 viewpointHandPosition = handSide == Side.Left ? thisPlayer.LeftHandPosition : thisPlayer.RightHandPosition;
            return viewpointHandPosition;
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

        /// <summary>
        /// Apply the given sound effect to the set of touched loopies, by appending it to their effect lists.
        /// </summary>
        public void ApplySoundEffectToTouchedLoopies(EffectId effect)
        {
            foreach (LocalLoopie localLoopie in
                DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                    DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
            {
                if (touchedLoopieIds.Contains(localLoopie.DistributedObject.Id))
                {
                    localLoopie.AppendSoundEffect(effect);
                }
            }
        }
    }
}
