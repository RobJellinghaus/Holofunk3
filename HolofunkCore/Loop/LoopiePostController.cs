// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Perform;
using Holofunk.Sound;
using LiteNetLib.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// Iterate over all known performers, and set the IsTouched property of any loopies they claim to be touching.
    /// </summary>
    /// <remarks>
    /// This script must run after the local Performer has been updated with the lists of loopies it is touching.
    /// </remarks>
    public class LoopiePostController : MonoBehaviour
    {
        /// <summary>
        /// Cached list to avoid reallocating per frame.
        /// </summary>
        private List<DistributedId> allTouchedLoopieList = new List<DistributedId>();

        public void Update()
        {
            foreach (LocalPerformer localPerformer in
                DistributedObjectFactory.FindComponentInstances<LocalPerformer>(
                    DistributedObjectFactory.DistributedType.Performer))
            {
                uint[] touchedLoopieIds = localPerformer.GetPerformer().TouchedLoopieIdList;
                allTouchedLoopieList.AddRange(touchedLoopieIds.Select(id => new DistributedId(id)));
            }

            foreach (LocalLoopie localLoopie in
                DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                    DistributedObjectFactory.DistributedType.Loopie))
            {
                if (allTouchedLoopieList.Contains(localLoopie.DistributedObject.Id))
                {
                    localLoopie.IsTouched = true;
                }
            }
        }
    }
}
