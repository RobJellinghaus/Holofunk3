﻿// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
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
    public interface IDistributedPerformer : IDistributedInterface, IEffectable
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
    }
}
