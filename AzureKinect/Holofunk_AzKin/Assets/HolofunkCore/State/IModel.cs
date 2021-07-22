// Copyright by Rob Jellinghaus. All rights reserved.

namespace Holofunk.StateMachines
{
    /// <summary>
    /// Marker interface for objects which can be manipulated by state machines.
    /// </summary>
    /// <remarks>
    /// In practice we simply downcast these objects, relying on safe downcasting to catch errors (which are
    /// programmer fail-fast bugs if they occur).
    /// </remarks>
    public interface IModel
    {
    }
}
