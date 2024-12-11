/// Copyright by Rob Jellinghaus.  All rights reserved.

using DistributedStateLib;
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
using Holofunk.Shape;

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
        /// The currently held menu verb.
        /// </summary>
        private MenuVerb currentlyHeldVerb = MenuVerb.MakeRoot();

        /// <summary>
        /// The child GameObject describing the currently held MenuVerb (only if the verb is defined).
        /// </summary>
        private GameObject currentlyHeldVerbGameObject;

        /// <summary>
        /// The icon for the microphone, when the mike is next to the mouth and there is a menu verb.
        /// </summary>
        private GameObject mikeIcon;

        /// <summary>
        /// The icon for the player index, floating next to their head.
        /// </summary>
        private GameObject headIcon;

        /// <summary>
        /// The sprites which have been pushed by the current state (if any).
        /// </summary>
        private Stack<GameObject> pushedSprites = new Stack<GameObject>();

        /// <summary>
        /// Has any Update already happened?
        /// </summary>
        /// <remarks>
        /// This works around a CurrentlyHeldVerbGameObject initialization issue.
        /// </remarks>
        private bool firstUpdate = false;

        /// <summary>
        /// Is this player in 3D mode?
        /// </summary>
        /// <remarks>
        /// If not in 3D mode, then the player is in 2D mode, and the hand position is projected to a fixed plane
        /// at MagicNumbers.PlaneDistance2DMode meters.
        /// </remarks>
        private bool is3D = false;

        #endregion Fields

        #region Properties

        internal DistributedPerformer DistributedPerformer => GetComponent<DistributedPerformer>();

        /// <summary>
        /// for debugging only
        /// </summary>
        internal string HandStateMachineInstanceString => stateMachineInstance?.ToString() ?? "";

        internal MenuVerb CurrentlyHeldVerb
        {
            get { return currentlyHeldVerb; }
            set
            {
                if (currentlyHeldVerbGameObject != null)
                {
                    GameObject.Destroy(currentlyHeldVerbGameObject);
                    currentlyHeldVerbGameObject = null;
                }

                currentlyHeldVerb = value;

                currentlyHeldVerbGameObject = MenuLevel.CreateMenuItem(this.transform, currentlyHeldVerb.ShapeType, Vector3.zero, currentlyHeldVerb.NameFunc());
                    MenuLevel.ColorizeMenuItem(currentlyHeldVerbGameObject, Color.white);                
            }
        }

        internal void Set3DMode(bool is3DMode) => this.is3D = is3DMode;

        internal void PushSprite(ShapeType spriteType)
        {
            // Put the sprite in a hand-holder so it will be the right size.
            // This lets us set hand scale in one place, controlled by the Unity scene graph.
            GameObject spriteHolder = ShapeContainer.InstantiateShape(ShapeType.HandHolder, transform);
            GameObject sprite = ShapeContainer.InstantiateShape(spriteType, spriteHolder.transform);

            // if there was a previous sprite, then hide it.
            // if no previous sprite, hide the currently held game object
            if (pushedSprites.Count > 0)
            {
                pushedSprites.Peek().SetActive(false);
            }
            else if (currentlyHeldVerbGameObject != null)
            {
                currentlyHeldVerbGameObject.SetActive(false);
            }

            sprite.SetActive(true);
            pushedSprites.Push(spriteHolder);
        }

        internal void PopSprite()
        {
            if (pushedSprites.Count > 0)
            {
                GameObject popped = pushedSprites.Pop();
                GameObject.Destroy(popped);
            }
            
            if (pushedSprites.Count == 0)
            {
                currentlyHeldVerbGameObject.SetActive(true);
            }
        }

        internal bool IsTouchingLoopies => touchedLoopieIds.Count > 0;

        public List<DistributedId> TouchedLoopieIds => touchedLoopieIds;

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
                HoloDebug.Log($"pplus_list.Count is {HidManager.Instance?.pplus_list?.Count}, pplusIndex is {pplusIndex}, nothing to do");
                return false;
            }

            if (DistributedViewpoint.Instance == null || DistributedViewpoint.Instance.PlayerCount <= playerIndex)
            {
                // No Kinect player recognized yet.
                HoloDebug.Log(string.Format("No Kinect, nothing to do."));
                return false;
            }

            return true; 
        }

        /// <summary>
        /// The controller is starting up; initialize any standard objects.
        /// </summary>
        void Start()
        {
            headIcon = ShapeContainer.InstantiateShape(this.playerIndex == 0 ? ShapeType.Number1Sprite : ShapeType.Number2Sprite, transform);
            mikeIcon = ShapeContainer.InstantiateShape(ShapeType.HandHolder, transform);
            ShapeContainer.InstantiateShape(ShapeType.MicrophoneSprite, mikeIcon.transform);
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsUpdatable())
            {
                return;
            }

            if (!firstUpdate)
            {
                firstUpdate = true;

                // initialize us to "ready to record" visual state
                CurrentlyHeldVerb = MenuVerb.MakeRoot();
            }

            // Check all odd/even PPlus controllers. Since we don't support more than two players
            // (due to Kinect limitations), we give all even-index controllers to player 1 (e.g. pplusIndex 0)
            // and all odd-index controllers to player 2 (e.g. pplusIndex 1).
            for (int pplusLoopIndex = pplusIndex; pplusLoopIndex < HidManager.Instance.pplus_list.Count; pplusLoopIndex += 2)
            {
                PPlus thisPPlus = HidManager.Instance.pplus_list[pplusLoopIndex];
                if (thisPPlus == null)
                {
                    continue; // you never know, they might not be contiguous if some get unpaired
                }

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
                    stateMachineInstance = new ControllerStateMachineInstance(PPlusEvent.MikeUp, ControllerStateMachine.Instance, new PPlusModel(null, this, _ => { }));
                }

                // Dequeue any button events that are waiting on any of the PPluses that we consider ours.
                PPlus.ButtonEvent ppevt;
                while (thisPPlus.TryDequeueEvent(out ppevt))
                {
                    Debug.Log($"PPlus #{pplusLoopIndex} button event: button {ppevt.button}, down {ppevt.down}");
                    stateMachineInstance.OnNext(new PPlusEvent(ppevt.button, ppevt.down));
                }
            }

            // And update the state machine instance's model.
            // In practice this winds up calling an update action defined by the state entry action.
            stateMachineInstance.ModelUpdate();

            int playerIndex = this.playerIndex;
            PlayerState playerState;
            // Now, which Player is that?
            if (DistributedViewpoint.Instance != null
                && DistributedViewpoint.Instance.TryGetPlayerById((PlayerId)(playerIndex + 1), out playerState)
                && !float.IsNaN(playerState.HeadPosition.x))
            {
                // get the distance between the player's non-controller hand and head
                Vector3 playerHeadPos = playerState.HeadPosition;
                // 0.1m to the right from the camera's perspective
                headIcon.SetActive(true);
                headIcon.transform.localPosition = playerHeadPos + new Vector3(MagicNumbers.HeadToPlayerNumberDistance, 0, 0);
            }
            else
            {
                headIcon.SetActive(false);
            }

            // If there's a currentlyHeldVerbGameObject that's not root, update its name.
            // (Root is special and has its own nameless "white-dot-in-circle" sprite.)
            if (currentlyHeldVerbGameObject != null && currentlyHeldVerb.Kind != MenuVerbKind.Root)
            {
                MenuLevel.SetMenuItemName(currentlyHeldVerbGameObject, currentlyHeldVerb.NameFunc());
            }

            // aaand, cheesily, update whatever the heck game object we are "holding".
            // This will be CurrentlyHeldMenuVerbGameObject UNLESS we have any pushed sprites (which override whatever menu
            // was being held, during the state in which they're pushed).
            // Tension: keeping this an implementation detail of the controller, vs avoiding ux-specific logic in the controller.
            // TODO: make there be a darn gameobject for the controller hand already (then could just stick it to that and 
            // let Unity take care of it).
            GameObject handGameObject = currentlyHeldVerbGameObject;
            if (pushedSprites.Count > 0)
            {
                handGameObject = pushedSprites.Peek();
            }
            if (handGameObject != null)
            {
                Vector3 position = GetViewpointHandPosition();
                if (!float.IsNaN(position.x))
                {
                    handGameObject.transform.localPosition = position;
                }

                bool mikeToMouth = IsMikeNextToMouth();
                //bool isTouching = currentlyHeldVerb.Kind == MenuVerbKind.Prompt
                //    || (currentlyHeldVerb.Kind == MenuVerbKind.Touch && touchedLoopieIds.Count > 0)
                //    || (currentlyHeldVerb.Kind == MenuVerbKind.Level && currentlyHeldVerb.MayBePerformer && mikeToMouth)
                //    || (currentlyHeldVerb.Kind == MenuVerbKind.Level && touchedLoopieIds.Count > 0);
                //MenuLevel.ColorizeMenuItem(currentlyHeldVerbGameObject, isTouching ? Color.white : Color.grey);

                if (mikeToMouth)
                {
                    mikeIcon.SetActive(true);
                    PlayerState thisPlayer = DistributedViewpoint.Instance.GetPlayerByIndex(playerIndex);
                    Vector3 viewpointHandPosition = handSide == Side.Left ? thisPlayer.RightHandPosition : thisPlayer.LeftHandPosition;

                    mikeIcon.transform.localPosition = viewpointHandPosition;
                }
                else
                {
                    mikeIcon.SetActive(false);
                }
            }
            else
            {
                mikeIcon.SetActive(false);
            }
        }

        public bool IsMikeNextToMouth()
        {
            // Now apply the adjustment appropriately.
            // Is the microphone hand close to the performer's mouth?
            // First, which player ID are we?
            bool result = false;
            int playerIndex = this.playerIndex;
            PlayerState playerState;
            // Now, which Player is that?
            if (DistributedViewpoint.Instance != null
                && DistributedViewpoint.Instance.TryGetPlayerById((PlayerId)(playerIndex + 1), out playerState))
            {
                // get the distance between the player's non-controller hand and head
                Vector3 playerHeadPos = playerState.HeadPosition;
                Side handSide = this.handSide;
                Vector3 mikeHandPos = handSide == Side.Left ? playerState.RightHandPosition : playerState.LeftHandPosition;

                float dist = Vector3.Distance(playerHeadPos, mikeHandPos);
                result = dist < MagicNumbers.MaximumHeadToMikeHandDistance;
                //HoloDebug.Log($"PPlusController.IsMikeNextToMouth(): playerHeadPos {playerHeadPos}, mikeHandPos {mikeHandPos}, dist {dist}, result {result}");
            }

            return result;
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

                // and push to the performer for distribution
                DistributedPerformer.SetTouchedLoopies(touchedLoopieIds.ToArray());
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
            PerformerState performerState = GetComponent<LocalPerformer>().GetState();

            GameObject newLoopie = DistributedLoopie.Create(
                viewpointHandPosition,
                audioInputId,
                default(DistributedId),
                performerState.Effects,
                performerState.EffectLevels,
                isPlaybackBackwards: false);
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

            // TODO: if in 2D mode, then project ray from origin through viewpointHandPosition, get intercept point with plane,
            // and make *that* be viewpointHandPosition.
            if (is3D)
            {
                // leave viewpointHandPosition alone
            }
            else
            {
                // Treat the viewpoint hand position as a vector, and scale it by the ratio of the hand's Z position
                // to the 2D plane's Z-depth.
                // This winds up setting viewpointHandPosition.z to MagicNumbers.PlaneDistance2DMode, thereby making
                // the hand position be on that plane.
                float scaleFactor = viewpointHandPosition.z / MagicNumbers.PlaneDistance2DMode;
                viewpointHandPosition /= scaleFactor;
            }

            return viewpointHandPosition;
        }
    }

    class PPlusModel : BaseModel<PPlusModel>
    {
        public PPlusController Controller { get; private set; }

        public PPlusModel(IModel parent, PPlusController controller, Action<PPlusModel> updateAction) : base(parent, updateAction)
        {
            Controller = controller;
        }
    }
}
