// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// Poll events on this object.
    /// </summary>
    /// <remarks>
    /// In testing this framework it is useful to be able to poll objects generically; this interface facilitates this.
    /// </remarks>
    public interface IPollEvents
    {
        void PollEvents();
    }
}
