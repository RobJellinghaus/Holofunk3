// Copyright (c) 2020 by Rob Jellinghaus.
using System;
using System.Collections.Generic;

namespace Distributed.State.Test
{
    /// <summary>
    /// Work queue that runs work when polled.
    /// </summary>
    /// <remarks>
    /// This is a basic implementation; the IWorkQueue interface exists to allow others as well.
    /// </remarks>
    class WorkQueue : IWorkQueue
    {
        /// <summary>
        /// The list of actions to run.
        /// </summary>
        private readonly List<Action> actions;

        public WorkQueue()
        {
            actions = new List<Action>();
        }

        /// <summary>
        /// The number of queued Actions.
        /// </summary>
        public int Count => actions.Count;

        /// <summary>
        /// Add the action; ignore the requested delay.
        /// </summary>
        /// <remarks>
        /// For testing purposes it is easier to just force the queued work deterministically.
        /// </remarks>
        public void RunLater(Action action, int delayMsec)
        {
            actions.Add(action);
        }

        /// <summary>
        /// Execute the queued work.
        /// </summary>
        /// <remarks>
        /// This is an odd method name, but it follows LiteNetLib precedent for event-pumped objects that are driven by
        /// externally serialized PollEvents calls.
        /// </remarks>
        public void PollEvents()
        {
            // allocate copy so that if any actions queue more work, it gets queued after all current work
            // TODO: pool these copies
            Action[] actionsCopy = actions.ToArray();
            actions.Clear();
            foreach (Action action in actionsCopy)
            {
                action();
            }
        }
    }
}
