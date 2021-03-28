// Copyright (c) 2020 by Rob Jellinghaus.
using Distributed.State;
using System;
using System.Collections.Generic;

namespace Distributed.Thing
{
    /// <summary>
    /// A trivial local object implementation that just keeps a bounded queue of integers.
    /// </summary>
    public class LocalThing : ILocalObject, IThing
    {
        /// <summary>
        /// Maximum number of values tracked.
        /// </summary>
        public static readonly int Capacity = 10;

        private Queue<int> state = new Queue<int>();

        private char[] lastMessage = null;

        private DistributedThing distributedThing;

        public LocalThing(int[] values = null)
        {
            Enqueue(values ?? new int[] { });
        }

        public void Enqueue(int[] values)
        {
            Contract.Requires(values != null);
            Contract.Requires(values.Length <= Capacity);

            while (values.Length + state.Count > Capacity)
            {
                state.Dequeue();
            }
            foreach (int value in values)
            {
                state.Enqueue(value);
            }
        }

        public void Ping(char[] message)
        {
            lastMessage = message;
        }

        public IEnumerable<int> LocalValues => state;

        public char[] LastMessage => lastMessage;

        public DistributedObject DistributedObject => distributedThing;

        public int Id => DistributedObject?.Id ?? 0;

        public void Delete()
        {
            // do nothing... accept the void
        }

        public void Initialize(DistributedObject distributedObject)
        {
            Contract.Requires(distributedObject != null);
            Contract.Requires(this.distributedThing == null);
            Contract.Requires(distributedObject is DistributedThing);

            this.distributedThing = (DistributedThing)distributedObject;
        }
    }
}
