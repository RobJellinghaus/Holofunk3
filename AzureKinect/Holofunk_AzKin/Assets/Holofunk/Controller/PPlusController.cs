﻿/// Copyright by Rob Jellinghaus.  All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Loop;
using Holofunk.Menu;
using Holofunk.Perform;
using Holofunk.Sound;
using Holofunk.StateMachines;
using Holofunk.Viewpoint;
using Holofunk.LevelWidget;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Controller
{
    using ControllerStateMachineInstance = StateMachineInstance<PPlusEvent>;

    /// <summary>
    /// Tracks the state and interactions of a single player's Presenter Plus controller.
    /// </summary>
    /// <remarks>
    /// This is even more horrible code duplication compared to PPlusController, but that will probably
    /// cease to exist soon.
    /// </remarks>
    public class PPlusController : MonoBehaviour, IModel
    {
        #region Fields

        /// <summary>
        /// Index of the JoyCon selected by this Controller; -1 means "unknown".
        /// </summary>
        /// <remarks>
        /// This is the index into the HidManager.Instance.j array. (Yes, HidManager
        /// uses public instance variables... oh well)
        /// </remarks>
        [Tooltip("PPlus index in HidManager")]
        public int pplusIndex = 0;

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
        /// The selected icon for this hand.
        /// </summary>
        private Option<GameObject> HandIcon;

        /// <summary>
        /// The loopie currently being held by this controller.
        /// </summary>
        /// <remarks>
        /// This is only ever non-null when the stateMachineInstance is in recording state.
        /// 
        /// TODO: generalize this to a set of held loopies? if we support copying in bulk? In fact isn't this just
        /// the touched loopie set?
        /// </remarks>
        private GameObject currentlyHeldLoopie;

        /// <summary>
        /// The menu this controller is manipulating, if any.
        /// </summary>
        /// <remarks>
        /// Keeping this as closure state across the state machine instance was clever but invalid.
        /// </remarks>
        private GameObject currentlyOpenMenu;

        /// <summary>
        /// What should this controller do when it touches a loopie?
        /// </summary>
        /// <remarks>
        /// This will be applied repeatedly on every update in which a loopie is touched, so idempotency is strongly recommended!
        /// This is used for e.g. handling muting / unmuting to each newly touched loopie.
        /// </remarks>
        private Action<DistributedLoopie> touchedLoopieAction;

        /// <summary>
        /// What should this controller do on every update, before touching loopies?
        /// </summary>
        /// <remarks>
        /// This is used for, e.g., updating the current level widget and performing other interactions that aren't
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

        #endregion

        #region Updates and actions

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

        #endregion

        /// <summary>
        /// Get the DistributedPerformer component from our parent.
        /// </summary>
        internal DistributedPerformer DistributedPerformer => GetComponent<DistributedPerformer>();

        /// <summary>
        /// for debugging only
        /// </summary>
        internal string HandStateMachineInstanceString => stateMachineInstance?.ToString() ?? "";

        public bool IsUpdatable()
        { 
            if (pplusIndex == -1 || HidManager.Instance == null || HidManager.Instance.pplus_list.Count <= pplusIndex){
                // No PPlus yet associated with this player; nothing to do.
                HoloDebug.Log($"pplus_list.Count is {HidManager.Instance.pplus_list.Count}, pplusIndex is {pplusIndex}, nothing to do");
                return false;
            }

            if (DistributedViewpoint.Instance == null || DistributedViewpoint.Instance.PlayerCount <= playerIndex)
            {
                // No Kinect player recognized yet.
                Debug.Log(string.Format("No Kinect, nothing to do."));
                return false;
            }

            return true; 
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsUpdatable())
            {
                return;
            }

            PPlus thisPPlus = HidManager.Instance.pplus_list[pplusIndex];

            // Joycon bailed, we're done
            if (thisPPlus.state == PPlus.State.DROPPED)
            {
                HoloDebug.Log($"pplusIndex {pplusIndex}; thisPPlus.state == DROPPED; closing and exiting.");
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
                stateMachineInstance = new ControllerStateMachineInstance(PPlusEvent.MikeUp, ControllerStateMachine.Instance, this);
            }

            // Dequeue any button events that are waiting.
            PPlus.ButtonEvent ppevt;
            while (thisPPlus.TryDequeueEvent(out ppevt))
            {
                Debug.Log($"PPlus button event: button {ppevt.button}, down {ppevt.down}");
                stateMachineInstance.OnNext(new PPlusEvent(ppevt.button, ppevt.down));
            }

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
                //Debug.Log($"Updated viewport hand position of loopie {currentlyHeldLoopie} to {viewpointHandPosition}");
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
                HoloDebug.Log($"Updating {handSide} touched loopies: prior [{string.Join(", ", previouslyTouchedLoopieIds)}], current [{string.Join(", ", touchedLoopieIds)}]");
                previouslyTouchedLoopieIds.Clear();
                previouslyTouchedLoopieIds.AddRange(touchedLoopieIds);
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

        #region Creation

        /// <summary>
        /// Create a new loopie at the current hand's position, and set it as the currentlyHeldLoopie.
        /// </summary>
        public void CreateLoopie(NowSoundLib.AudioInputId audioInputId)
        {
            Holofunk.Core.Contract.Requires(currentlyHeldLoopie == null);

            if (DistributedViewpoint.Instance == null)
            {
                HoloDebug.Log("No DistributedViewpoint.TheViewpoint; can't create loopie");
                return;
            }

            Vector3 viewpointHandPosition = GetViewpointHandPosition();

            GameObject newLoopie = DistributedLoopie.Create(viewpointHandPosition, audioInputId);
            currentlyHeldLoopie = newLoopie;
        }

        public GameObject CreateMenu(MenuKinds menuKind)
        {
            HoloDebug.Log($"Creating menu kind {menuKind} for pplusController #{playerIndex}{handSide}");

            // get the forward direction towards the camera from the hand location
            Vector3 localHandPosition = GetViewpointHandPosition();

            Vector3 viewpointHandPosition = localHandPosition;
            // was previously: DistributedViewpoint.Instance.LocalToViewpointMatrix().MultiplyPoint(localHandPosition);

            Vector3 viewpointForwardDirection = Vector3.forward;

            HoloDebug.Assert(currentlyOpenMenu == null, "Must not already be an open menu for this controller");

            currentlyOpenMenu = DistributedMenu.Create(
                menuKind,
                viewpointForwardDirection,
                viewpointHandPosition);

            currentlyOpenMenu.GetComponent<MenuController>().Initialize(this);

            return currentlyOpenMenu;
        }

        public GameObject CurrentlyOpenMenu => currentlyOpenMenu;

        public void CloseOpenMenu()
        {
            HoloDebug.Assert(currentlyOpenMenu != null, "Must be an open menu to close");

            currentlyOpenMenu = null;
        }

        /// <summary>
        /// Create a level widget.
        /// </summary>
        public GameObject CreateLevelWidget()
        {
            if (DistributedViewpoint.Instance == null)
            {
                HoloDebug.Log("No DistributedViewpoint.TheViewpoint; can't create level widget");
                return null;
            }

            Vector3 viewpointHandPosition = GetViewpointHandPosition();

            GameObject newWidget = DistributedLevelWidget.Create(viewpointHandPosition);
            return newWidget;
        }

        #endregion

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
    }
}
