// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using Holofunk.Viewpoint;
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

        private Vector3 lastViewpointPosition = Vector3.zero;

        internal void Initialize(Loopie loopie)
        {
            this.loopie = loopie;
        }

        /// <summary>
        /// Get the loopie's state.
        /// </summary>
        public Loopie GetLoopie() => loopie;

        public void Update()
        {
            // move to viewpoint position if viewpoint position moved (and/or viewpoint matrix came back)
            if (lastViewpointPosition != loopie.ViewpointPosition)
            {
                if (DistributedViewpoint.TheViewpoint != null)
                {
                    Matrix4x4 viewpointToLocalMatrix = DistributedViewpoint.TheViewpoint.ViewpointToLocalMatrix();
                    Vector3 localLoopiePosition = viewpointToLocalMatrix.MultiplyPoint(loopie.ViewpointPosition);
                    lastViewpointPosition = loopie.ViewpointPosition;

                    transform.localPosition = localLoopiePosition;
                }
            }
        }

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

        public void SetViewpointPosition(Vector3 viewpointPosition)
        {
            loopie.ViewpointPosition = viewpointPosition;
        }

        public void FinishRecording()
        {
            // TODO
        }
    }
}
