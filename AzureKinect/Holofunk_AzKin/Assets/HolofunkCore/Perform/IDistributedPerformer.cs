// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using Holofunk.Sound;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// The distributed interface of a performer.
    /// </summary>
    /// <remarks>
    /// Each HoloLens 2 version of Holofunk in the current system will host its own DistributedPerformer,
    /// which it uses to disseminate state about what that performer is doing.
    /// </remarks>
    public interface IDistributedPerformer : IDistributedInterface
    {
        /// <summary>
        /// The state of the Performer.
        /// </summary>
        PerformerState GetState();

        /// <summary>
        /// Set the loopies touched by this performer.
        /// </summary>
        /// <param name="touchedLoopies"></param>
        void SetTouchedLoopies(DistributedId[] touchedLoopies);

        /// <summary>
        /// Alter a sound effect on the performer.
        /// </summary>
        void AlterSoundEffect(EffectId effectId, int initialLevel, int alteration, bool commit);

        /// <summary>
        /// Pop the most recently created sound effect from this performer.
        /// </summary>
        void PopSoundEffect();

        /// <summary>
        /// Clear all sound effects from this performer.
        /// </summary>
        void ClearSoundEffects();
    }
}
