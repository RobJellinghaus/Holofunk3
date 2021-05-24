// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// General interface implemented by all distributed objects.
    /// </summary>
    /// <remarks>
    /// Each type of distributed object implements an interface which derives from this;
    /// that interface represents the methods that can be invoked on these objects.
    /// Since all objects can be deleted, all such interfaces include this one's Delete method.
    /// </remarks>
    public interface IDistributedInterface
    {
        /// <summary>
        /// Invoked whenever a distributed object is deleted.
        /// </summary>
        /// <remarks>
        /// Note that this can occur either because the owner object itself was deleted, or because
        /// a peer disconnected and local proxies had to be deleted.
        /// </remarks>
        void Delete();
    }
}
