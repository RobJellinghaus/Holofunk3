// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Distributed;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// Set the IsTouched property of all local loopies to false.
    /// </summary>
    /// <remarks>
    /// The visual representation of whether a loopie is touched is computed each frame anew, based on the
    /// current lists of loopies that each known performer claims to be touching. The purpose of this class
    /// is to initialize that per-frame recomputation by starting off with all loopies being visually untouched.
    /// </remarks>
    public class LoopiePreController : MonoBehaviour
    {
        public void Update()
        {
            foreach (LocalLoopie localLoopie in
                DistributedObjectFactory.FindComponentInstances<LocalLoopie>(
                    DistributedObjectFactory.DistributedType.Loopie))
            {
                localLoopie.IsTouched = false;
            }
        }
    }
}
