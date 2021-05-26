// Copyright (c) 2020 by Rob Jellinghaus.

using LiteNetLib;

namespace Distributed.State
{
    /// <summary>
    /// A distributed object.
    /// </summary>
    /// <remarks>
    /// Each type of distributed object implements an interface which derives from this;
    /// that interface represents the methods that can be invoked on these objects.
    /// Since all objects can be deleted, all such interfaces include this one's Delete method.
    /// </remarks>
    public interface IDistributedObject : IDistributedInterface
    {
        /// <summary>
        /// The host that contains this object.
        /// </summary>
        DistributedHost Host { get; }

        /// <summary>
        /// The NetPeer which owns this proxy, if this is a proxy.
        /// </summary>
        NetPeer OwningPeer { get; }

        /// <summary>
        /// Is this object the original, owning instance?
        /// </summary>
        /// <remarks>
        /// Owner objects relay commands to proxies, along with updating local state;
        /// proxy objects relay command requests to owners, and update local state only on commands from owners.
        /// </remarks>
        bool IsOwner { get; }

        /// <summary>
        /// The id of this object; unique within its owner.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The local object which implements the local behavior of the distributed object.
        /// </summary>
        ILocalObject LocalObject { get; }

        /// <summary>
        /// Delete this distributed object.
        /// </summary>
        /// <remarks>
        /// If the owner, this causes the owning object and all proxies to be deleted. If a proxy, this
        /// causes a delete request to be sent to the owner.
        /// </remarks>
        void Delete();

        /// <summary>
        /// This DistributedObject's host has become unreachable.
        /// </summary>
        /// <remarks>
        /// TODO: is this potentially a reversible condition? Could there be an OnAttach?
        /// </remarks>
        void OnDetach();

        IDistributedType DistributedType { get; }
    }
}
