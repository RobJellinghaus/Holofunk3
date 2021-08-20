// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
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
        PerformerState GetPerformer();

        /// <summary>
        /// Update the performer.
        /// </summary>
        void UpdatePerformer(PerformerState performer);
    }
}
