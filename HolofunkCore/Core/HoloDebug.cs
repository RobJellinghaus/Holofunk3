/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

using System;
using System.Diagnostics;

namespace Holofunk.Core
{
    class HoloDebugException : Exception
    {
        public HoloDebugException(string message) : base(message) { }
    }

    public class HoloDebug
    {
        public static void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void Assert(bool condition, string message = "")
        {
            if (!condition)
            {
                Log("Assertion violated");
                throw new HoloDebugException(message);
            }
        }
    }
}
