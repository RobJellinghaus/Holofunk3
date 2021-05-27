// Copyright (c) 2020 by Rob Jellinghaus.

using LiteNetLib;

namespace Distributed.State
{
    /// <summary>
    /// A distributed object.
    /// </summary>
    /// <remarks>
    /// Each implementation of an owner or proxy implements this interface.
    /// </remarks>
    public interface IDistributedObject : IDistributedInterface
    {
        /// <summary>
        /// The host that contains this representation of this object.
        /// </summary>
        /// <remarks>
        /// The Host is a singleton which tracks all the locally known owners and proxies in a
        /// single DistributedState session.
        /// </remarks>
        DistributedHost Host { get; }

        /// <summary>
        /// The ID of this object (as defined by its owning host -- objects from different hosts may have
        /// the same DistributedId values).
        /// </summary>
        DistributedId Id { get; }

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

        /// <summary>
        /// Get access to the operations needed for creating and deleting objects of this type.
        /// </summary>
        IDistributedType DistributedType { get; }
    }
}
