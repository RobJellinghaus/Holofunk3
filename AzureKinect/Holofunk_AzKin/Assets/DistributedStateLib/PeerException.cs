﻿// Copyright (c) 2020 by Rob Jellinghaus.
using System;

namespace DistributedStateLib
{
    /// <summary>
    /// Exception thrown due to unexpected condition in Peer code.
    /// </summary>
    public class PeerException : Exception
    {
        public PeerException(string message) : base(message) { }
    }
}
