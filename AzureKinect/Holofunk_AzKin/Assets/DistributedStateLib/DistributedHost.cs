// Copyright (c) 2020 by Rob Jellinghaus.
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

// Give test suite (only) visibility into internal properties of this assembly, 
// and especially this DistributedPeer type.
[assembly: InternalsVisibleTo("DistributedStateTest")]

namespace Distributed.State
{
    /// <summary>
    /// Peer participant in the distributed system.
    /// </summary>
    /// <remarks>
    /// This encapsulates a LiteNetLib NetManager instance, used for both broadcast discovery
    /// and update, and reliable peer-to-peer communication.
    /// 
    /// The Peer's first responsibility is discovering other Peers. 
    /// </remarks>
    public class DistributedHost : IPollEvents, IDisposable
    {
        private static string RequestKey = "";

        #region Listener inner class

        /// <summary>
        /// Listener handles unconnected messages (broadcasts), as well as connected
        /// messages from peers.
        /// </summary>
        private class Listener : INetEventListener
        {
            readonly DistributedHost Host;
            internal Listener(DistributedHost host)
            {
                Host = host;
            }
            public void OnConnectionRequest(ConnectionRequest request)
            {
                request.AcceptIfKey(RequestKey);
            }

            public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
            {
                // TBD what to do here
            }

            public void OnNetworkLatencyUpdate(NetPeer netPeer, int latency)
            {
                // TBD whether anything should be done here
            }

            public void OnNetworkReceive(NetPeer netPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
            {
                Host.netPacketProcessor.ReadAllPackets(reader, netPeer);
            }

            public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
            {
                Host.netPacketProcessor.ReadAllPackets(reader, remoteEndPoint);
            }

            public void OnPeerConnected(NetPeer netPeer)
            {
                Host.AddPeer(netPeer);
            }

            /// <summary>
            /// Handle a disconnected peer by deleting all that peer's proxies' local objects, and dropping
            /// all the proxies.
            /// </summary>
            public void OnPeerDisconnected(NetPeer netPeer, DisconnectInfo disconnectInfo)
            {
                SerializedSocketAddress peerAddress = new SerializedSocketAddress(netPeer);
                if (Host.proxies.TryGetValue(peerAddress, out Dictionary<DistributedId, IDistributedObject> peerObjects))
                {
                    // detach them all
                    foreach (IDistributedObject proxy in peerObjects.Values)
                    {
                        proxy.OnDetach();
                    }

                    // and drop the whole collection of proxies
                    Host.proxies.Remove(peerAddress);
                }
            }
        }

        #endregion

        #region Capability for adding proxies

        /// <summary>
        /// Capability class that allows callbacks to register proxies, but no one else.
        /// </summary>
        public class ProxyCapability
        {
            public readonly DistributedHost Host;
            internal ProxyCapability(DistributedHost host)
            {
                Host = host;
            }
            public void AddProxy(NetPeer netPeer, IDistributedObject newProxy)
            {
                Host.AddProxy(new SerializedSocketAddress(netPeer), newProxy);
            }
            public void SubscribeReusable<TMessage, TUserData>(Action<TMessage, TUserData> action)
                where TMessage : class, new()
            {
                Host.SubscribeReusable(action);
            }
            public void OnDelete(NetPeer netPeer, DistributedId id, bool isRequest)
            {
                Host.OnDelete(netPeer, id, isRequest);
            }
        }

        #endregion

        #region Fields and properties

        /// <summary>
        /// Random port that happened to be, not only open, but with no other UDP or TCP ports in the 3????? range
        /// on my local Windows laptop.
        /// </summary>
        public static ushort DefaultListenPort = 30303;

        /// <summary>
        /// Random port that happened to be, not only open, but with no other UDP or TCP ports in the 3????? range
        /// on my local Windows laptop.
        /// </summary>
        public static ushort DefaultReliablePort = 30304;

        /// <summary>
        /// Delay between announce messages.
        /// </summary>
        public int AnnounceDelayMsec { get; set; }

        /// <summary>
        /// The broadcast port for announcing new peers and disseminating information.
        /// </summary>
        /// <remarks>
        /// Maybe shouldn't overload broadcast port; we'll see.
        /// </remarks>
        public readonly ushort ListenPort;

        /// <summary>
        /// The serialized socket address of this Peer.
        /// </summary>
        public readonly SerializedSocketAddress SocketAddress;

        /// <summary>
        /// The LiteNetLib instance for handling broadcast traffic; has no peers.
        /// </summary>
        private readonly NetManager netManager;

        /// <summary>
        /// Reusable NetDataWriter instance; reset before use.
        /// </summary>
        private readonly NetDataWriter netDataWriter;

        /// <summary>
        /// Packet processor that handles type mapping on the wire.
        /// </summary>
        private readonly NetPacketProcessor netPacketProcessor;

        /// <summary>
        /// The IWorkQueue used for scheduling future work.
        /// </summary>
        private readonly IWorkQueue workQueue;

        /// <summary>
        /// Map from owner object ID to owner object instance.
        /// </summary>
        /// <remarks>
        /// Note that these IDs are unique only within this Peer; each Peer defines its own ID space
        /// for its owned objects.
        /// </remarks>
        private readonly Dictionary<DistributedId, IDistributedObject> owners = new Dictionary<DistributedId, IDistributedObject>();

        /// <summary>
        /// The LiteNetLib Logger to use.
        /// </summary>
        private readonly INetLogger logger;

        /// <summary>
        /// The next id to assign to a new owning object.
        /// </summary>
        private uint nextOwnerId;

        /// <summary>
        /// Map from NetPeer to proxy ID to proxy instance.
        /// </summary>
        /// <remarks>
        /// Note that each ID is unique only within that peer's collection; each peer defines its
        /// own proxies' ID space.
        /// </remarks>
        private readonly Dictionary<SerializedSocketAddress, Dictionary<DistributedId, IDistributedObject>> proxies
            = new Dictionary<SerializedSocketAddress, Dictionary<DistributedId, IDistributedObject>>();

        /// <summary>
        /// How many self-announcements has this peer received?
        /// </summary>
        /// <remarks>
        /// Only for testing.
        /// </remarks>
        public int SelfAnnounceCount { get; private set; }

        /// <summary>
        /// How many peer announcements has this peer received?
        /// </summary>
        /// <remarks>
        /// Only for testing.
        /// </remarks>
        public int PeerAnnounceCount { get; private set; }

        /// <summary>
        /// How many peer announcement responses has this peer received?
        /// </summary>
        /// <remarks>
        /// Only for testing.
        /// </remarks>
        public int PeerAnnounceResponseCount { get; private set; }

        /// <summary>
        /// Number of currently connected peers.
        /// </summary>
        public int PeerCount => netManager.ConnectedPeersCount;

        /// <summary>
        /// Number of peers for which proxies exist. (testing only)
        /// </summary>
        internal int ProxyPeerCount => proxies.Count;

        /// <summary>
        /// Map from object IDs to owner objects. (testing only)
        /// </summary>
        public IReadOnlyDictionary<DistributedId, IDistributedObject> Owners => owners;

        /// <summary>
        /// Collection of connected peers.
        /// </summary>
        public IEnumerable<NetPeer> NetPeers => netManager.ConnectedPeerList;

        /// <summary>
        /// Endpoints that have responded to an announcement, to prevent repeated connect requests.
        /// </summary>
        /// <remarks>
        /// Once endpoints are connected, they are removed from this list (so if a disconnection happens,
        /// they can be re-announced and re-responded to).
        /// </remarks>
        internal List<IPEndPoint> AnnouncedEndPoints = new List<IPEndPoint>();

        /// <summary>
        /// Get the proxies that are owned by this peer; returns null if peer is unknown.
        /// </summary>
        public IReadOnlyDictionary<DistributedId, IDistributedObject> ProxiesForPeer(SerializedSocketAddress serializedSocketAddress)
        {
            /* this contract does not hold if proxies drop. Original intent was to make sure data structures were consistent.
            Contract.Requires(netManager.ConnectedPeerList.Any(peer =>
            {
                SerializedSocketAddress peerAddress = new SerializedSocketAddress(peer);
                bool addressesAreEqual = peerAddress.Equals(serializedSocketAddress);
                return addressesAreEqual;
            }));

            Contract.Requires(proxies.ContainsKey(serializedSocketAddress));
            */

            if (proxies.ContainsKey(serializedSocketAddress))
            {
                return proxies[serializedSocketAddress];
            }
            else
            {
                return null;
            }
        }


        #endregion

        #region Construction, type registration, and disposal

        public DistributedHost(
            IWorkQueue workQueue,
            ushort listenPort,
            bool isListener = true,
            int disconnectTimeout = -1,
            INetLogger logger = null)
        {
            Contract.Requires(listenPort != 0);

            ListenPort = listenPort;
            AnnounceDelayMsec = 2000; // default value, works OK for interactive discovery
            this.workQueue = workQueue;
            this.logger = logger;

            // determine our IP
            // hat tip https://stackoverflow.com/questions/6803073/get-local-ip-address
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            IPAddress ipv4Address = host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            SocketAddress = new SerializedSocketAddress(new IPEndPoint(ipv4Address, listenPort).Serialize());

            netManager = new NetManager(new Listener(this))
            {
                BroadcastReceiveEnabled = true,
                UnconnectedMessagesEnabled = true
            };

            if (disconnectTimeout != -1)
            {
                netManager.DisconnectTimeout = disconnectTimeout;
            }

            netPacketProcessor = new NetPacketProcessor();
            RegisterType(SerializedSocketAddress.Serialize, SerializedSocketAddress.Deserialize);
            RegisterType<DistributedId>();
            netPacketProcessor.SubscribeReusable<AnnounceMessage, IPEndPoint>(OnAnnounceReceived);
            netPacketProcessor.SubscribeReusable<AnnounceResponseMessage, IPEndPoint>(OnAnnounceResponseReceived);

            netDataWriter = new NetDataWriter();

            bool managerStarted;
            if (isListener)
            {
                managerStarted = netManager.Start(ListenPort);
            }
            else
            {
                managerStarted = netManager.Start();
            }

            if (!managerStarted)
            {
                throw new PeerException("Could not start netManager");
            }
        }

        /// <summary>
        /// Dispose of this Peer, reclaiming network resources.
        /// </summary>
        public void Dispose()
        {
            netManager.Stop(true);
        }

        public void RegisterType<T>()
            where T : struct, INetSerializable
        {
            netPacketProcessor.RegisterNestedType<T>();
        }

        public void RegisterType<T>(Action<NetDataWriter, T> writerFunc, Func<NetDataReader, T> readerFunc)
        {
            netPacketProcessor.RegisterNestedType(writerFunc, readerFunc);
        }

        #endregion

        #region Managing DistributedObjects

        public void RegisterWith(Action<DistributedHost.ProxyCapability> registrar)
        {
            registrar(new ProxyCapability(this));
        }

        /// <summary>
        /// Get the next owner ID for this DistributedPeer.
        /// </summary>
        /// <remarks>
        /// This allows external code to create its own owner DistributedObjects, giving them fresh
        /// IDs at construction.
        /// </remarks>
        internal DistributedId NextOwnerId()
        {
            return ++nextOwnerId;
        }

        /// <summary>
        /// This is a new owner DistributedObject entering the system on this peer.
        /// </summary>
        internal void AddOwner(IDistributedObject distributedObject)
        {
            owners.Add(distributedObject.Id, distributedObject);
            
            // and tell all the peers
            // TODO: invert this so we create the message, write it to NetDataWriter, and then just blast it to all peers
            foreach (NetPeer netPeer in netManager.ConnectedPeerList)
            {
                distributedObject.DistributedType.SendCreateMessageInternal(netPeer);
            }
        }

        /// <summary>
        /// Delete this owner object from this Peer's tables.
        /// </summary>
        internal void Delete(IDistributedObject distributedObject, Action<NetPeer, bool> sendDeleteMessage)
        {
            Contract.Requires(distributedObject != null);
            Contract.Requires(sendDeleteMessage != null);
            Contract.Requires(distributedObject.Host == this);

            if (distributedObject.IsOwner)
            {
                owners.Remove(distributedObject.Id);
                // TODO: invert this so we create the message once and blast it to all peers
                foreach (NetPeer netPeer in NetPeers)
                {
                    sendDeleteMessage(netPeer, false);
                }
            }
            else
            {
                sendDeleteMessage(distributedObject.OwningPeer, true);
            }
        }

        /// <summary>
        /// This is a new proxy being created on this peer, owned by netPeer.
        /// </summary>
        private void AddProxy(SerializedSocketAddress peerAddress, IDistributedObject proxy)
        {
            Contract.Requires(!proxy.IsOwner);

            proxies[peerAddress].Add(proxy.Id, proxy);
        }

        /// <summary>
        /// Add a subscription for this message type.
        /// </summary>
        /// <remarks>
        /// Since the only polymorphism supported by LiteNetLib is for packets (e.g. messages),
        /// adding new DistributedObject implementations requires adding new messages, which requires
        /// subscribing to those new messages.
        /// </remarks>
        private void SubscribeReusable<TMessage, TUserData>(Action<TMessage, TUserData> action)
            where TMessage : class, new()
        {
            netPacketProcessor.SubscribeReusable(action);
        }

        private void OnDelete(NetPeer netPeer, DistributedId id, bool isRequest)
        {
            SerializedSocketAddress peerAddress = new SerializedSocketAddress(netPeer);
            if (isRequest)
            {
                // owner id may or may not still exist; it's OK if it doesn't.
                if (!owners.ContainsKey(id))
                {
                    // do nothing; ignore the delete request altogether
                }
                else
                {
                    // we will accept this request... for testing purposes.
                    // TBD if this is the right thing in general!
                    IDistributedObject distributedObject = owners[id];

                    // and tell all proxies
                    foreach (NetPeer proxyPeer in NetPeers)
                    {
                        distributedObject.DistributedType.SendDeleteMessageInternal(proxyPeer, false);
                    }

                    owners.Remove(id);
                    distributedObject.OnDelete();
                }
            }
            else
            {
                // this is an authoritative delete message from the owner.
                // we expect strong consistency here so the id should still exist.
                Contract.Requires(proxies[peerAddress].ContainsKey(id));

                IDistributedObject proxy = proxies[peerAddress][id];
                proxies[peerAddress].Remove(id);
                proxy.Delete();
            }
        }

        #endregion

        #region Sending

        /// <summary>
        /// Send an announcement message, and schedule the next such message.
        /// </summary>
        /// <remarks>
        /// After constructing a Peer, generally one calls Announce() just once
        /// to start the perpetual cycle of announcements that each Peer makes.
        /// </remarks>
        public void Announce(bool isHostingAudio = false)
        {
            logger?.WriteNet(NetLogLevel.Trace, $"DistributedHost.Announce()");

            AnnounceMessage message = new AnnounceMessage
            {
                AnnouncerSocketAddress = SocketAddress,
                AnnouncerIsHostingAudio = isHostingAudio,
                KnownPeers = netManager
                    .ConnectedPeerList
                    .Select(peer => new SerializedSocketAddress(peer.EndPoint.Serialize()))
                    .ToArray()
            };

            SendBroadcastMessage(message);

            // schedule next announcement
            workQueue.RunLater(() => Announce(isHostingAudio), AnnounceDelayMsec);
        }

        /// <summary>
        /// Send this message as a broadcast.
        /// </summary>
        public void SendBroadcastMessage<T>(T message)
            where T : class, new()
        {
            netDataWriter.Reset();
            netPacketProcessor.Write(netDataWriter, message);
            netManager.SendBroadcast(netDataWriter, ListenPort);
        }

        /// <summary>
        /// Send this message directly, as an unconnected message.
        /// </summary>
        private void SendUnconnectedMessage<T>(T message, IPEndPoint endpoint)
            where T : class, new()
        {
            netDataWriter.Reset();
            netPacketProcessor.Write(netDataWriter, message);
            netManager.SendUnconnectedMessage(netDataWriter, endpoint);
        }

        /// <summary>
        /// Send this message directly, as a reliable (sequenced) message.
        /// </summary>
        public void SendReliableMessage<T>(T message, NetPeer netPeer)
            where T : class, new()
        {
            netDataWriter.Reset();
            netPacketProcessor.Write(netDataWriter, message);
            netPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
        }

        /// <summary>
        /// Send create messages for all our owned objects to this peer.
        /// </summary>
        private void SendProxiesToPeer(NetPeer netPeer)
        {
            foreach (KeyValuePair<DistributedId, IDistributedObject> entry in owners)
            {
                entry.Value.DistributedType.SendCreateMessageInternal(netPeer);
            }
        }

        /// <summary>
        /// Send this message to all proxies.
        /// </summary>
        public void SendToProxies<T>(T message)
            where T : class, new()
        {
            netDataWriter.Reset();
            netPacketProcessor.Write(netDataWriter, message);
            foreach (NetPeer netPeer in NetPeers)
            {
                netPeer.Send(netDataWriter, DeliveryMethod.ReliableOrdered);
            }
        }

        private void AddPeer(NetPeer netPeer)
        {
            logger?.WriteNet(NetLogLevel.Trace, $"DistributedHost.AddPeer({netPeer.EndPoint}) -- {ConnectionsStatusString()}]");

            // we expect them to be in the connected peer list now
            Contract.Requires(netManager.ConnectedPeerList.Contains(netPeer));

            // Remove them from the list of announced endpoints (don't be too picky about whether they were in it or not)
            AnnouncedEndPoints.Remove(netPeer.EndPoint);

            proxies.Add(new SerializedSocketAddress(netPeer), new Dictionary<DistributedId, IDistributedObject>());

            SendProxiesToPeer(netPeer);
        }

        private static string AsString<T>(IEnumerable<T> enumerable, Func<T, string> toStringFunc)
        {
            if (enumerable == null)
            {
                return "-null-";
            }
            else
            {
                return string.Join(", ", enumerable.Select(t => toStringFunc(t)));
            }
        }

        private string ConnectionsStatusString()
        {
            return $"ConnectedPeerList: [{AsString<NetPeer>(netManager?.ConnectedPeerList, np => np.EndPoint.ToString())}], AnnouncedEndpoints: [{AsString<IPEndPoint>(this.AnnouncedEndPoints, ep => ep.ToString())}]";
        }

        #endregion

        #region Receiving

        /// <summary>
        /// An announcement has been received (via broadcast); react accordingly.
        /// </summary>
        private void OnAnnounceReceived(AnnounceMessage message, IPEndPoint endpoint)
        {
            // heed only ipv4 for now... TBD what to do about this
            if (endpoint.AddressFamily == AddressFamily.InterNetwork)
            {
                // is this actually our own announcement!?
                SerializedSocketAddress incomingAddress = new SerializedSocketAddress(endpoint.Serialize());
                if (incomingAddress == this.SocketAddress)
                {
                    // for testing only, really
                    SelfAnnounceCount++;
                    return;
                }

                PeerAnnounceCount++;

                // do we know this peer already?
                // (could happen in race scenario)
                if (netManager.ConnectedPeerList.Any(peer => peer.EndPoint.Equals(endpoint)))
                {
                    return;
                }

                // did we already respond to this peer?
                if (AnnouncedEndPoints.Contains(endpoint))
                {
                    return;
                }

                // did this peer know us already? (typical scenario given re-announcements)
                if (message.KnownPeers.Contains(SocketAddress))
                {
                    return;
                }

                logger?.WriteNet(NetLogLevel.Trace, $"DistributedHost.OnAnnounceReceived({endpoint}) -- responding. {ConnectionsStatusString()}]");

                // send announce response
                AnnounceResponseMessage response = new AnnounceResponseMessage { };
                SendUnconnectedMessage(response, endpoint);
            }
        }

        /// <summary>
        /// An announcement has been received (via unconnected message); react accordingly.
        /// </summary>
        private void OnAnnounceResponseReceived(AnnounceResponseMessage message, IPEndPoint endpoint)
        {
            // heed only ipv4 for now... TBD what to do about this
            if (endpoint.AddressFamily == AddressFamily.InterNetwork)
            {
                PeerAnnounceResponseCount++;

                logger?.WriteNet(NetLogLevel.Trace, $"DistributedHost.OnAnnounceResponseReceived({endpoint}) -- {ConnectionsStatusString()}");

                // we shouldn't know this peer yet, let's check.
                if (netManager.ConnectedPeerList.Any(peer => peer.EndPoint.Equals(endpoint)))
                {
                    // surprise, we do! must have been a race.
                    return;
                }

                if (AnnouncedEndPoints.Contains(endpoint))
                {
                    return;
                }

                AnnouncedEndPoints.Add(endpoint);

                logger?.WriteNet(NetLogLevel.Info, $"DistributedHost.OnAnnounceResponseReceived({endpoint}) -- sending connect request");

                // So, connect away. (Note this could still race -- Connect is thread-safe but the
                // AddPeer method is not. TODO: look at fixing this.)
                // Deliberately ignore the netPeer return value; we let the OnPeerConnected event carry the ball.
                netManager.Connect(endpoint, RequestKey);
            }
        }

        /// <summary>
        /// Poll all network events pending.
        /// </summary>
        /// <remarks>
        /// Called from game thread; calls to this must not be concurrent with work that has
        /// been queued on the work queue.
        /// </remarks>
        public void PollEvents()
        {
            netManager.PollEvents();
        }

        #endregion
    }
}
