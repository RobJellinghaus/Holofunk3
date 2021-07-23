/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Hand;
using Holofunk.Loopie;
using Holofunk.StateMachines;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.HandComponents
{
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
        private Option<HandPose> lastHandPose;

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
        private DistributedLoopie currentlyHeldLoopie;

        /// <summary>
        /// The radius of the hand in world space.
        /// </summary>
        /// <remarks>
        /// Determined from the X scale of the HollowSprite circle.
        /// </remarks>
        private float handRadius;

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

        internal void CreateLoopie()
        {
        }

        internal void ReleaseLoopie()
        { 
        }
    }
}
