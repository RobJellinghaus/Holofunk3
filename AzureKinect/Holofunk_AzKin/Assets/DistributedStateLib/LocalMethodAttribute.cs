// Copyright (c) 2020 by Rob Jellinghaus.

using System;

namespace Distributed.State
{
    /// <summary>
    /// This method is implemented purely locally, with no distributed communication.
    /// </summary>
    /// <remarks>
    /// This attribute isn't validated or used in code generation or anything else, yet; it's more for documentation
    /// at the moment.
    /// </remarks>
    [System.AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = true)]
    public class LocalMethodAttribute : Attribute
    {
    }
}
