// Copyright (c) 2020 by Rob Jellinghaus.
using System;

namespace Distributed.State
{
    /// <summary>
    /// Allow scheduling work for later.
    /// </summary>
    /// <remarks>
    /// Since this may be running under Unity where threading is not a thing, but we don't want to
    /// hardcode Unity coroutines etc. into this library either, we use this interface to abstract over
    /// however the local environment supports running work later.
    /// 
    /// The assumption is that work invoked from this queue will never race with other PollEvents calls
    /// on other objects, in particular other LiteNetLib objects.    
    /// </remarks>
    public interface IWorkQueue : IPollEvents
    {
        /// <summary>
        /// Execute this action sometime in the future by some means.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <param name="delayMsec">Delay before executing.</param>
        void RunLater(Action action, int delayMsec);
    }
}
