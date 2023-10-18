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
        /// <remarks>
        /// Yes, this is terrible, to have this duplicated; just defining the types should be enough.
        /// </remarks>
        public enum DistributedType
        {
            Loopie,
            Menu,
            Performer,
            SoundClock,
            SoundEffect,
            Viewpoint,
            LevelWidget,
        }

        /// <summary>
        /// The types of distributed interfaces currently known.
        /// </summary>
        /// <remarks>
        /// Yes, this is terrible, to have this duplicated; just defining the interfaces should be enough.
        /// </remarks>
        public enum DistributedInterface
        {
            IEffectable,
        }

        /// <summary>
        /// Find the prototype GameObject to be cloned for a given type.
        /// </summary>
        /// <remarks>
        /// Note that the prototype may vary structurally across apps.
        /// </remarks>
        public static GameObject FindPrototypeContainer(DistributedType type)
        {
            GameObject prototypeContainer = GameObject.Find(DistributedObjectPrototypes);
            Contract.Requires(prototypeContainer != null);
            Transform child = prototypeContainer.transform.Find(type.ToString());
            Contract.Requires(child != null);
            return child.gameObject;
        }

        public static T FindPrototypeComponent<T>(DistributedType type)
        {
            GameObject obj = FindPrototypeContainer(type);
            return obj.GetComponent<T>();
        }

        // Get the first Player in the first Viewpoint in the first currently connected peer.
        public static T FindFirstInstanceComponent<T>(DistributedType type)
            where T : class
            => FindFirstInstanceContainer(type)?.GetComponent<T>();

        /// <summary>
        /// Find the first parent GameObject for new object instances of the given type.
        /// </summary>
        /// <remarks>
        /// This just assumes the first endpoint we find is the one we want.
        /// </remarks>
        public static GameObject FindFirstInstanceContainer(DistributedType type)
            => FindComponentContainers(type, false).FirstOrDefault();

        /// <summary>
        /// Find the parent GameObject for new object instances of the given type from the given peer.
        /// </summary>
        /// <remarks>
        /// Under the DistributedObjectInstances top-level object (which must exist), this method creates
        /// a child container named after the host endpoint, and then a grandchild container named after the
        /// object's type. Net effect should be a Unity hierarchy that nicely shows what objects came from
        /// where.
        /// </remarks>
        private static GameObject FindInstanceContainer(DistributedType type, string id)
        {
            Transform instanceContainer = GameObject.Find(DistributedObjectInstances).transform;
            Contract.Requires(instanceContainer != null);

            // find or create child game object for this host
            Transform hostContainer = instanceContainer.Find(id);
            if (hostContainer == null)
            {
                GameObject newContainerObject = new GameObject(id);
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
        /// Find the parent GameObject for new object instances of the given type from the given peer.
        /// </summary>
        /// <remarks>
        /// Under the DistributedObjectInstances top-level object (which must exist), this method creates
        /// a child container named after the host endpoint, and then a grandchild container named after the
        /// object's type. Net effect should be a Unity hierarchy that nicely shows what objects came from
        /// where.
        /// </remarks>
        public static GameObject FindProxyInstanceContainer(DistributedType type, NetPeer netPeer)
            => FindInstanceContainer(type, netPeer.EndPoint.ToString());

        /// <summary>
        /// Find the parent GameObject for new object instances of the given type from the given peer.
        /// </summary>
        /// <remarks>
        /// Under the DistributedObjectInstances top-level object (which must exist), this method creates
        /// a child container named after the host endpoint, and then a grandchild container named after the
        /// object's type. Net effect should be a Unity hierarchy that nicely shows what objects came from
        /// where.
        /// </remarks>
        public static GameObject FindLocalhostInstanceContainer(DistributedType type)
            => FindInstanceContainer(type, "localhost");

        public static IEnumerable<T> FindComponentInstances<T>(DistributedType type, bool includeActivePrototype)
            where T : Component
            => FindComponentContainers(type, includeActivePrototype).Select(gameobj => gameobj.GetComponent<T>());

        /// <summary>
        /// Enumerate all components of the given object type across all known instances.
        /// </summary>
        /// <remarks>
        /// TODO: look at whether this ever becomes a performance hot spot because everyone hates LINQ in Unity.
        /// But look at how simple this app is, surely if any app can afford it, Holofunk can!
        /// </remarks>
        public static IEnumerable<GameObject> FindComponentContainers(DistributedType type, bool includeActivePrototype)
        {
            if (includeActivePrototype)
            {
                GameObject prototype = FindPrototypeContainer(type);
                if (prototype.activeSelf)
                {
                    yield return prototype;
                }
            }

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
                        yield return instanceChild.gameObject;
                    }
                }
            }
        }
    }
}
