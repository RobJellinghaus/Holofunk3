/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.HandComponents;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Holofunk.Perform
{
    /// <summary>
    /// This behavior determines which player maps to this performer.
    /// </summary>
    public class PerformerPreController : MonoBehaviour
    {
        private PlayerState ourPlayer = default(PlayerState);
        /// <summary>
        /// Get the state of our Player instance.
        /// </summary>
        /// <remarks>
        /// Returned by ref for efficiency, since Player is a large struct.
        /// </remarks>
        public ref PlayerState OurPlayer => ref ourPlayer;

        // Update is called once per frame
        public void Update()
        {
            // Look for our matching Player.
            // if we don't have a player with our host address, then we aren't recognized yet,
            // so do nothing.
            // TODO: handle multiple Players.
            LocalViewpoint localViewpoint = DistributedObjectFactory.FindFirstInstanceComponent<LocalViewpoint>(
                DistributedObjectFactory.DistributedType.Viewpoint);
            ourPlayer = default(PlayerState);
            if (localViewpoint != null)
            {
                if (DistributedViewpoint.Instance == null)
                {
                    DistributedViewpoint.InitializeTheViewpoint(localViewpoint.GetComponent<DistributedViewpoint>());
                }

                if (localViewpoint.PlayerCount > 0)
                {
                    ourPlayer = localViewpoint.GetPlayer(0);
                }
            }
            else
            {
                // wipe it
                DistributedViewpoint.InitializeTheViewpoint(null);
            }
        }
    }
}
