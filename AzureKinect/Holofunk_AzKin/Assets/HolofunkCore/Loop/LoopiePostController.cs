// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Controller;
using Holofunk.Distributed;
using Holofunk.Perform;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// Iterate over all known performers or JoyonControllers, and set the IsTouched property of any loopies that anything
    /// claims to be touching.
    /// </summary>
    /// <remarks>
    /// This script must run after the local Performers and JoyconControllers have been updated with the lists of loopies
    /// they are touching.
    /// </remarks>
    public class LoopiePostController : MonoBehaviour
    {
        /// <summary>
        /// Cached list to avoid reallocating per frame.
        /// </summary>
        private HashSet<DistributedId> allTouchedLoopieSet = new HashSet<DistributedId>();

        public void Update()
        {
            allTouchedLoopieSet.Clear();

            foreach (LocalPerformer localPerformer in
                DistributedObjectFactory.FindComponentInstances<LocalPerformer>(
                    DistributedObjectFactory.DistributedType.Performer, includeActivePrototype: true))
            {
                uint[] touchedLoopieIds = localPerformer.GetState().TouchedLoopieIdList;
                allTouchedLoopieSet.UnionWith(touchedLoopieIds.Select(id => new DistributedId(id)));
            }

            foreach (LocalLoopie localLoopie in
                DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                    DistributedObjectFactory.DistributedType.Loopie, includeActivePrototype: false))
            {
                if (allTouchedLoopieSet.Contains(localLoopie.DistributedObject.Id))
                {
                    localLoopie.IsTouched = true;
                }
            }
        }
    }
}
