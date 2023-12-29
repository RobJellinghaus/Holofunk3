// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Core;
using Holofunk.Loop;
using Holofunk.Perform;
using Holofunk.Sound;
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
        public static readonly string Players = nameof(Players);

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
            Core.Contract.Requires(prototypeContainer != null);
            Transform child = prototypeContainer.transform.Find(type.ToString());
            Core.Contract.Requires(child != null);
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
            => FindComponentContainers(new DistributedType[] { type }, false).FirstOrDefault();

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
            Core.Contract.Requires(instanceContainer != null);

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
            => FindComponentContainers(new DistributedType[] { type }, includeActivePrototype).Select(gameobj => gameobj.GetComponent<T>());

        /// <summary>
        /// Map a collection of IDs to a particular kind of object into the set of those objects.
        /// </summary>
        /// <remarks>
        /// Used especially when copying, to allow creating all new objects (from these collected ones) without concurrent iteration.
        /// </remarks>
        public static HashSet<T> CollectDistributedComponents<T>(DistributedType type, HashSet<DistributedId> ids)
            where T : DistributedComponent
            => new HashSet<T>(FindComponentContainers(new DistributedType[] { type }, false)
                .Select(gameobj => gameobj.GetComponent<T>())
                .Where(component => ids.Contains(component.Id)));
                
        // TODO: make this not so terribly hardcoded to just the one cross-type interface that exists
        public static IEnumerable<IEffectable> FindComponentInterfaces()
            // there is only one interface right now and we know Loopies and Performers are it
            => FindComponentContainers(new DistributedType[] { DistributedType.Loopie, DistributedType.Performer }, false)
                .Select(gameobj =>
                {
                    DistributedLoopie loopie = gameobj.GetComponent<DistributedLoopie>();
                    if (loopie == null)
                    {
                        DistributedPerformer performer = gameobj.GetComponent<DistributedPerformer>();
                        return (IEffectable)performer;
                    }
                    else
                    {
                        return (IEffectable)loopie;
                    }
                });

        /// <summary>
        /// Enumerate all components of the given object type across all known instances.
        /// </summary>
        /// <remarks>
        /// TODO: look at whether this ever becomes a performance hot spot because everyone hates LINQ in Unity.
        /// But look at how simple this app is, surely if any app can afford it, Holofunk can!
        /// </remarks>
        public static IEnumerable<GameObject> FindComponentContainers(DistributedType[] types, bool includeActivePrototype)
        {
            if (includeActivePrototype)
            {
                foreach (DistributedType type in types)
                {
                    GameObject prototype = FindPrototypeContainer(type);
                    if (prototype.activeSelf)
                    {
                        yield return prototype;
                    }
                }
            }

            Transform instanceContainer = GameObject.Find(DistributedObjectInstances).transform;
            Core.Contract.Requires(instanceContainer != null);

            for (int i = 0; i < instanceContainer.childCount; i++)
            {
                Transform child = instanceContainer.GetChild(i);

                foreach (DistributedType type in types)
                {
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

            // and look at the Players container to pick up the local Performers
            // TODO: make this not so terribly hacky... maybe there should be a general Id->DistributedObject
            // (owner OR proxy) lookup function on the DistributedHost?
            Transform playersContainer = GameObject.Find(Players).transform;
            Core.Contract.Requires(playersContainer != null);
            foreach (DistributedType type in types)
            {
                if (type == DistributedType.Performer)
                {
                    for (int i = 0; i < playersContainer.childCount; i++)
                    {
                        Transform playerChild = playersContainer.GetChild(i);
                        yield return playerChild.gameObject;
                    }
                }
            }
        }
    }
}
