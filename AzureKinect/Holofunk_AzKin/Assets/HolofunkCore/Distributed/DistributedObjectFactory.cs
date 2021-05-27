﻿/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Distributed
{
    /// <summary>
    /// Utility methods for DistributedObject construction under Unity.
    /// </summary>
    public static class DistributedObjectFactory
    {
        public static readonly string DistributedObjectPrototypes = nameof(DistributedObjectPrototypes);
        public static readonly string DistributedObjectInstances = nameof(DistributedObjectInstances);

        /// <summary>
        /// The types of distributed objects currently known.
        /// </summary>
        public enum DistributedType
        {
            Viewpoint,
        }

        /// <summary>
        /// Find the prototype GameObject to be cloned for a given type.
        /// </summary>
        /// <remarks>
        /// Note that the prototype may vary structurally across apps.
        /// </remarks>
        public static GameObject FindPrototype(DistributedType type)
        {
            GameObject prototypeContainer = GameObject.Find(DistributedObjectPrototypes);
            Contract.Requires(prototypeContainer != null);
            Transform child = prototypeContainer.transform.Find(type.ToString());
            Contract.Requires(child != null);
            return child.gameObject;
        }

        /// <summary>
        /// Find the parent GameObject for new object instances of the given type.
        /// </summary>
        public static GameObject FindContainer(DistributedType type, NetPeer netPeer)
        {
            Transform instanceContainer = GameObject.Find(DistributedObjectInstances).transform;
            Contract.Requires(instanceContainer != null);

            // find or create child game object for this host
            string hostName = netPeer.EndPoint.ToString();
            Transform hostContainer = instanceContainer.Find(hostName);
            if (hostContainer == null)
            {
                GameObject newContainerObject = new GameObject(hostName);
                newContainerObject.transform.SetParent(instanceContainer.transform);
                hostContainer = newContainerObject.transform;
            }

            Transform typeContainer = hostContainer.Find(type.ToString());
            if (typeContainer == null)
            {
                GameObject newTypeObject = new GameObject(type.ToString());
                newTypeObject.transform.SetParent(hostContainer);
                typeContainer = newTypeObject.transform;
            }

            return typeContainer.gameObject;
        }
    }
}