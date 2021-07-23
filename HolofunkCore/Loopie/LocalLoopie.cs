// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Loopie
{
    /// <summary>
    /// The local implementation of a Loopie object.
    /// </summary>
    public class LocalLoopie : MonoBehaviour, IDistributedLoopie, ILocalObject
    {
        /// <summary>
        /// The state of this Loopie, in distributed terms.
        /// </summary>
        private Loopie loopie;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedLoopie>();

        internal void Initialize(Loopie loopie)
        {
            this.loopie = loopie;
        }

        /// <summary>
        /// Get the loopie's state.
        /// </summary>
        public Loopie GetLoopie() => loopie;

        public void OnDelete()
        {
            // Go gently
        }

        public void SetMute(bool isMuted)
        {
            loopie.IsMuted = isMuted;
        }

        public void SetVolume(float volume)
        {
            loopie.Volume = volume;
        }
    }
}
