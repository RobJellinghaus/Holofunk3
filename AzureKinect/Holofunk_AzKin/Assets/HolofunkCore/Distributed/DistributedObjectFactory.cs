// Copyright by Rob Jellinghaus. All rights reserved.

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
            Performer,
            Loopie,
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

        public static T FindPrototypeComponent<T>(DistributedType type)
        {
            GameObject obj = FindPrototype(type);
            return obj.GetComponent<T>();
        }

        // Get the first Player in the first Viewpoint in the first currently connected peer.
        public static T FindFirstInstanceComponent<T>(DistributedType type)
            where T : class
        {
            GameObject ic = FindFirstContainer(type);
            
            if (ic != null)
            {
                // get the first viewpoint out of it
                if (ic.transform.childCount > 0)
                {
                    return ic.transform.GetChild(0).GetComponent<T>();
                }
            }

            return null;
        }

        /// <summary>
        /// Find the first parent GameObject for new object instances of the given type.
        /// </summary>
        /// <remarks>
        /// This just assumes the first endpoint we find is the one we want.
        /// </remarks>
        public static GameObject FindFirstContainer(DistributedType type)
        {
            Transform instanceContainer = GameObject.Find(DistributedObjectInstances).transform;
            Contract.Requires(instanceContainer != null);

            if (instanceContainer.childCount == 0)
            {
                return null;
            }

            Transform hostContainer = instanceContainer.GetChild(0);

            Transform typeContainer = hostContainer.Find(type.ToString());

            return typeContainer?.gameObject;
        }

        /// <summary>
        /// Find the parent GameObject for new object instances of the given type from the given peer.
        /// </summary>
        /// <remarks>
        /// Under the DistributedObjectInstances top-level object (which must exist), this method creates
        /// a child container named after the host endpoint, and then a grandchild container named after the
        /// object's type. Net effect should be a Unity hierarchy that nicely shows what objects came from
        /// where.
        /// </remarks>
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

        /// <summary>
        /// Enumerate all components of the given object type across all known instances.
        /// </summary>
        /// <remarks>
        /// TODO: look at whether this ever becomes a performance hot spot because everyone hates LINQ in Unity.
        /// But look at how simple this app is, surely if any app can afford it, Holofunk can!
        /// </remarks>
        public static IEnumerable<T> FindComponentInstances<T>(DistributedType type)
            where T : Component
        {
            Transform instanceContainer = GameObject.Find(DistributedObjectInstances).transform;
            Contract.Requires(instanceContainer != null);

            for (int i = 0; i < instanceContainer.childCount; i++)
            {
                Transform child = instanceContainer.GetChild(i);

                Transform typeChild = child.Find(type.ToString());

                if (typeChild != null)
                {
                    for (int j = 0; j < typeChild.childCount; j++)
                    {
                        Transform instanceChild = typeChild.GetChild(j);
                        yield return instanceChild.gameObject.GetComponent<T>();
                    }
                }
            }
        }
    }
}
