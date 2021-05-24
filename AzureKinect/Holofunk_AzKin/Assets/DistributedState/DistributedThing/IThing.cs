// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.Thing
{
    using Distributed.State;
    using System.Collections.Generic;

    public interface IThing : IDistributedInterface
    {
        /// <summary>
        /// Enqueue these values.
        /// </summary>
        /// <remarks>
        /// Each Thing keeps a bounded queue of values.
        /// 
        /// As with all non-Local IDistributedInterface messages, calling this on a proxy will
        /// result in a request to the owner object.
        /// </remarks>
        [ReliableMethod]
        void Enqueue(int[] values);

        /// <summary>
        /// Get the local values held by this Thing, in order from first enqueued to last.
        /// </summary>
        [LocalMethod]
        IEnumerable<int> LocalValues { get; }

        /// <summary>
        /// Ping all other instances of this object with this message.
        /// </summary>
        /// <param name="message"></param>
        [BroadcastMethod]
        void Ping(char[] message);

        /// <summary>
        /// Get the last message that was pinged.
        /// </summary>
        [LocalMethod]
        char[] LastMessage { get; }
    }
}
