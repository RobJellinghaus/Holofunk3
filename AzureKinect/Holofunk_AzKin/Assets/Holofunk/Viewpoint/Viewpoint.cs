/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using com.rfilkov.components;
using com.rfilkov.kinect;
using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
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
    }
}
