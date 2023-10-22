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
using System.Collections;

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
    public class PPlusController : MonoBehaviour
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
        /// The MenuVerb currently held by this controller, if any was picked from the menu.
        /// </summary>
        /// <remarks>
        /// This is used for rendering the verb name over the user's controller hand, and for determining the
        /// variety of action that gets applied by the verb when the user hits the LIGHT button.
        /// </remarks>
        private MenuVerb currentlyHeldVerb;

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

        #endregion Fields

        #region Properties

        internal DistributedPerformer DistributedPerformer => GetComponent<DistributedPerformer>();

        /// <summary>
        /// for debugging only
        /// </summary>
        internal string HandStateMachineInstanceString => stateMachineInstance?.ToString() ?? "";

        internal bool AnyLoopiesTouched => touchedLoopieIds.Count > 0;

        internal MenuVerb CurrentlyHeldVerb => currentlyHeldVerb;

        public IModel Parent => null;

        #endregion

        #region Updates and actions

        /// <summary>
        /// Any loopies touched by this controller?
        /// </summary>
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
                stateMachineInstance = new ControllerStateMachineInstance(PPlusEvent.MikeUp, ControllerStateMachine.Instance, new PPlusModel(this, _ => { }));
            }

            // Dequeue any button events that are waiting.
            PPlus.ButtonEvent ppevt;
            while (thisPPlus.TryDequeueEvent(out ppevt))
            {
                Debug.Log($"PPlus button event: button {ppevt.button}, down {ppevt.down}");
                stateMachineInstance.OnNext(new PPlusEvent(ppevt.button, ppevt.down));
            }

            // And update the state machine instance's model.
            // In practice this winds up calling an update action defined by the state entry action.
            stateMachineInstance.ModelUpdate();
        }

        /// <summary>
        /// Update the local lists of loopies touched by this hand.
        /// </summary>
        public void UpdateTouchedLoopieList()
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
        /// <param name="audioInputId">The audio input to record from (all loopies are created in record mode).</param>
        public GameObject CreateLoopie(NowSoundLib.AudioInputId audioInputId)
        {
            if (DistributedViewpoint.Instance == null)
            {
                HoloDebug.Log("No DistributedViewpoint.TheViewpoint; can't create loopie");
                return null;
            }

            Vector3 viewpointHandPosition = GetViewpointHandPosition();

            GameObject newLoopie = DistributedLoopie.Create(viewpointHandPosition, audioInputId);
            return newLoopie;
        }

        /// <summary>
        /// Create a menu instance held by this PPlusController.
        /// </summary>
        /// <returns>the GameObject for the menu instance</returns>
        public GameObject CreateMenu()
        {
            HoloDebug.Log($"Creating menu for pplusController #{playerIndex}{handSide}");

            // get the forward direction towards the camera from the hand location
            Vector3 localHandPosition = GetViewpointHandPosition();

            Vector3 viewpointHandPosition = localHandPosition;
            // was previously: DistributedViewpoint.Instance.LocalToViewpointMatrix().MultiplyPoint(localHandPosition);

            Vector3 viewpointForwardDirection = Vector3.forward;

            GameObject currentlyOpenMenu = DistributedMenu.Create(
                viewpointForwardDirection,
                viewpointHandPosition);

            currentlyOpenMenu.GetComponent<MenuController>().Initialize(this);

            return currentlyOpenMenu;
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
    }

    class PPlusModel : RootModel<PPlusModel>
    {
        public PPlusController Controller { get; private set; }

        public PPlusModel(PPlusController controller, Action<PPlusModel> updateAction) : base(updateAction)
        {
            Controller = controller;
        }
    }
}
