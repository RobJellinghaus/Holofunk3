// Copyright (c) 2020 by Rob Jellinghaus.
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;

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

        private readonly INetLogger logger;

        public WorkQueue(INetLogger logger = null)
        {
            actions = new List<(long, Action)>();
            this.logger = logger;
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
            long targetTimeMsec = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + delayMsec;

            logger?.WriteNet(NetLogLevel.Trace, $"Calling RunLater, delayMsec {delayMsec}, targetTimeMsec {targetTimeMsec}, times [{string.Join(", ", actions.Select(t => t.Item1.ToString()))}]");

            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].Item1 >= targetTimeMsec)
                {
                    actions.Insert(i, (targetTimeMsec, action));

                    CheckOrder(actions);

                    return;
                }
            }

            actions.Add((targetTimeMsec, action));

            CheckOrder(actions);
        }

        private void CheckOrder(List<(long, Action)> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                Contract.Requires(list[i - 1].Item1 <= list[i].Item1);
            }
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
                    // we can run this now
                    temporaryQueue.Add(actions[i]);
                }
                else
                {
                    break;
                }
            }

            actions.RemoveRange(0, temporaryQueue.Count);

            CheckOrder(actions);
            CheckOrder(temporaryQueue);

            foreach (var action in temporaryQueue)
            {
                action.Item2();
            }

            logger?.WriteNet(NetLogLevel.Trace, $"WorkQueue.PollEvents(): at end: actions.Count {actions.Count}");
        }
    }
}
