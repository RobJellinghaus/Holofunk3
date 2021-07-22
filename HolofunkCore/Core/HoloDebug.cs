// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib;
using System;
using System.Diagnostics;

namespace Holofunk.Core
{
    class HoloDebugException : Exception
    {
        public HoloDebugException(string message) : base(message) { }
    }

    public class HoloDebug : INetLogger
    {
        public static HoloDebug Instance = new HoloDebug();

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

        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            Log($"[{level}] {string.Format(str, args)}");
        }
    }
}
