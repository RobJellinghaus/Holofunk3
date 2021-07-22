// Copyright by Rob Jellinghaus. All rights reserved.

using com.rfilkov.components;
using com.rfilkov.kinect;
using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Perform;
using UnityEngine;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// Static methods for convenience of accessing singleton Viewpoint.
    /// </summary>
    public static class Viewpoint
    {
        public static DistributedViewpoint TheInstance
        {
            get
            {
                GameObject viewpointPrototype = DistributedObjectFactory.FindPrototype(DistributedObjectFactory.DistributedType.Viewpoint);
                DistributedViewpoint distributedViewpoint = viewpointPrototype.GetComponent<DistributedViewpoint>();
                return distributedViewpoint;
            }
        }

        /// <summary>
        /// Get the number of Performers that currently have local proxies.
        /// </summary>
        /// <remarks>
        /// Performers only ever exist as proxies on Azure Kinect hosts.
        /// </remarks>
        public static int GetPerformerCount()
        {
            GameObject performerContainer = DistributedObjectFactory.FindFirstContainer(DistributedObjectFactory.DistributedType.Performer);
            // if no container, then ain't no performers
            return performerContainer?.transform.childCount ?? 0;
        }

        public static DistributedPerformer GetPerformer(int performerIndex)
        {
            GameObject performerContainer = DistributedObjectFactory.FindFirstContainer(DistributedObjectFactory.DistributedType.Performer);
            GameObject distributedPerformerGameObject = performerContainer.transform.GetChild(performerIndex).gameObject;
            return distributedPerformerGameObject.GetComponent<DistributedPerformer>();
        }
    }
}
