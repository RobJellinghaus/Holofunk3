// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using LiteNetLib;
using System;
using System.Net;
using UnityEngine;

namespace Holofunk.Distributed
{
    public class Registrar : Messages
    {
        /// <summary>
        /// Register this kind of create message for this local object.
        /// </summary>
        /// <typeparam name="TCreateMessage">The create message type</typeparam>
        /// <typeparam name="TDistributed">The distributed object type</typeparam>
        /// <typeparam name="TLocal">The local object type</typeparam>
        /// <param name="proxyCapability">The registration capability</param>
        /// <param name="distributedType">The distributed type enum value</param>
        /// <param name="localAction">The action to initialize the local object with the create message's state</param>
        public static void RegisterCreateMessage<TMessage, TDistributed, TLocal, TInterface>(
            DistributedHost.ProxyCapability proxyCapability,
            DistributedObjectFactory.DistributedType distributedType,
            Action<TLocal, TMessage> localAction)
            where TMessage : ReliableMessage, new()
            where TDistributed : DistributedComponent, TInterface
            where TLocal : ILocalObject, TInterface
            where TInterface : IDistributedInterface
        {
            proxyCapability.SubscribeReusable((TMessage message, NetPeer netPeer) =>
            {
                // get the prototype object
                GameObject prototype = DistributedObjectFactory.FindPrototype(distributedType);
                GameObject parent = DistributedObjectFactory.FindContainer(distributedType, netPeer);
                GameObject clone = UnityEngine.Object.Instantiate(prototype, parent.transform);

                clone.name = $"{message.Id}";
                clone.SetActive(true);

                HoloDebug.Log($"Received {message} for id {message.Id} from peer {netPeer.EndPoint}");

                // wire the local and distributed things together
                TLocal local = clone.GetComponent<TLocal>();
                TDistributed distributed = clone.GetComponent<TDistributed>();

                localAction(local, message);
                distributed.InitializeProxy(netPeer, message.Id);

                proxyCapability.AddProxy(netPeer, distributed);
            });
        }

        /// <summary>
        /// Register a reliable message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <typeparam name="TDistributed">The distributed type</typeparam>
        /// <typeparam name="TLocal">The local type</typeparam>
        /// <typeparam name="TInterface">The distributed interface</typeparam>
        /// <param name="proxyCapability">The registration capability</param>
        public static void RegisterReliableMessage<TMessage, TDistributed, TLocal, TInterface>(
            DistributedHost.ProxyCapability proxyCapability)
            where TMessage : ReliableMessage, new()
            where TDistributed : IDistributedObject, TInterface
            where TLocal : ILocalObject, TInterface
            where TInterface : IDistributedInterface
        {
            proxyCapability.SubscribeReusable((TMessage message, NetPeer netPeer) =>
                HandleReliableMessage<TMessage, TDistributed, TLocal, TInterface>(
                    proxyCapability.Host,
                    netPeer,
                    message));
        }

        /// <summary>
        /// Register a broadcast message type.
        /// </summary>
        /// <typeparam name="TMessage">The message type</typeparam>
        /// <typeparam name="TDistributed">The distributed type</typeparam>
        /// <typeparam name="TLocal">The local type</typeparam>
        /// <typeparam name="TInterface">The distributed interface</typeparam>
        /// <param name="proxyCapability">The registration capability</param>
        public static void RegisterBroadcastMessage<TMessage, TDistributed, TLocal, TInterface>(
            DistributedHost.ProxyCapability proxyCapability)
            where TMessage : BroadcastMessage, new()
            where TDistributed : IDistributedObject, TInterface
            where TLocal : ILocalObject, TInterface
            where TInterface : IDistributedInterface
        {
            proxyCapability.SubscribeReusable((TMessage message, IPEndPoint endPoint) =>
                HandleBroadcastMessage<TMessage, TDistributed, TLocal, TInterface>(proxyCapability.Host, message));
        }
    }
}
