// Copyright (c) 2020 by Rob Jellinghaus.
using System;
using System.Collections.Generic;

namespace Distributed.State
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
        /// The list of actions to run and timestamps to run them at.
        /// </summary>
        /// <remarks>
        /// The long quantities are milliseconds.
        /// </remarks>
        private readonly List<(long, Action)> actions;

        public WorkQueue()
        {
            actions = new List<(long, Action)>();
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
        public void RunLater(Action action, int delayMsec = 0)
        {
            // 0 means "ASAP"
            long targetTimeMsec = delayMsec == 0 ? 0 : (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + delayMsec;
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].Item1 > delayMsec)
                {
                    actions.Insert(i, (targetTimeMsec, action));
                    return;
                }
            }
            actions.Add((targetTimeMsec, action));
        }

        private List<(long, Action)> temporaryQueue = new List<(long, Action)>();

        /// <summary>
        /// Execute the queued work.
        /// </summary>
        /// <remarks>
        /// This is an odd method name, but it follows LiteNetLib precedent for event-pumped objects that are driven by
        /// externally serialized PollEvents calls.
        /// </remarks>
        public void PollEvents()
        {
            long currentTimeInMsec = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            temporaryQueue.Clear();

            // copy all the actions that can run now to temporaryQueue
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].Item1 <= currentTimeInMsec)
                {
                    // we can run this
                    temporaryQueue.Add(actions[i]);
                }
                else
                {
                    // here's where we stop
                    actions.RemoveRange(0, i);
                    break;
                }
            }
            foreach (var action in temporaryQueue)
            {
                if (action.Item1 <= currentTimeInMsec)
                {
                    action.Item2();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
