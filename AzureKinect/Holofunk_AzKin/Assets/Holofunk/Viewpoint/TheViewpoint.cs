// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Perform;
using System.Linq;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Static methods for convenience of accessing singleton Viewpoint.
    /// </summary>
    public static class TheViewpoint
    {
        public static DistributedViewpoint Instance => DistributedObjectFactory.FindPrototypeComponent<DistributedViewpoint>(
            DistributedObjectFactory.DistributedType.Viewpoint);

        /// <summary>
        /// Get the number of Performers that currently have local proxies.
        /// </summary>
        /// <remarks>
        /// Performers only ever exist as proxies on Azure Kinect hosts.
        /// </remarks>
        public static int GetPerformerCount()
            => DistributedObjectFactory.FindComponentContainers(
                DistributedObjectFactory.DistributedType.Performer, includeActivePrototype: false)
            .Count();

        public static DistributedPerformer GetPerformer(int performerIndex)
            => DistributedObjectFactory.FindComponentInstances<DistributedPerformer>(
                DistributedObjectFactory.DistributedType.Performer, includeActivePrototype: false)
            .ElementAt(performerIndex);
    }
}
