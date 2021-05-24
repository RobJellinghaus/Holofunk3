// Copyright (c) 2020 by Rob Jellinghaus.

using System;

namespace Distributed.State
{
    /// <summary>
    /// This method is implemented via unreliable broadcast to all other hosts.
    /// </summary>
    /// <remarks>
    /// This attribute isn't validated or used in code generation or anything else, yet; it's more for documentation
    /// at the moment.
    /// </remarks>
    [System.AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class BroadcastMethodAttribute : Attribute
    {
    }
}
