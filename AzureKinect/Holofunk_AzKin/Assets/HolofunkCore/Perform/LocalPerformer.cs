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
    /// The local implementation of a Viewpoint object.
    /// </summary>
    /// <remarks>
    /// This keeps the local list of all players for this distributed viewpoint object on this host,
    /// whether this host is the owning host or not.
    /// </remarks>
    public class LocalPerformer : MonoBehaviour, IDistributedPerformer, ILocalObject
    {
        /// <summary>
        /// We keep the players list completely unsorted for now.
        /// </summary>
        private PerformerState performer;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedPerformer>();

        internal void Initialize(PerformerState performer)
        {
            this.performer = performer;
        }

        /// <summary>
        /// Get the (singular) performer.
        /// </summary>
        public PerformerState GetPerformer() => performer;

        public void OnDelete()
        {
            // Go gently
        }

        /// <summary>
        /// Update the performer.
        /// </summary>
        public void UpdatePerformer(PerformerState performer)
        {
            this.performer = performer;
        }
    }
}
