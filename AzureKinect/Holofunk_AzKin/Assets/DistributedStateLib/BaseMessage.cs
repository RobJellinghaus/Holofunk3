// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// Base message class containing Id and IsRequest properties.
    /// </summary>
    public abstract class BaseMessage
    {
        /// <summary>
        /// Id of this object (in the originating peer's ID space).
        /// </summary>
        public DistributedId Id { get; set; }

        public BaseMessage()
        { }

        public BaseMessage(DistributedId id)
        {
            Id = id;
        }

        /// <summary>
        /// Invoke the message on the given target, which may be a distributed object or a local object.
        /// </summary>
        public abstract void Invoke(IDistributedInterface target);
    }
}
