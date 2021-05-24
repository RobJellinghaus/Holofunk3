// Copyright (c) 2020 by Rob Jellinghaus.
using System.Diagnostics;

namespace Distributed.State
{
    public static class Contract
    {
        public static void Requires(bool condition, string message = null)
        {
            if (!condition)
            {
                Debug.Assert(condition, message);
            }
        }
    }
}
