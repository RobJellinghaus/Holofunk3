// Copyright (c) 2020 by Rob Jellinghaus.

using LiteNetLib;

namespace Distributed.State
{
    /// <summary>
    /// Operations on a particular distributed type.
    /// </summary>
    /// <remarks>
    /// Each type of distributed object implements an interface which derives from this,
    /// and a singleton object implementing that interface. This lets us factor out meta-operations
    /// (like create and delete) from the ordinary interface of the type.
    /// </remarks>
    public interface IDistributedInterface
    {
        /// <summary>
        /// This distributed object has been deleted.
        /// </summary>
        void OnDelete();
    }
}
