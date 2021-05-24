// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// Base message class containing Id and IsRequest properties.
    /// </summary>
    public abstract class ReliableMessage : BaseMessage
    {
        /// <summary>
        /// Is this a request to delete (from a proxy)?
        /// </summary>
        /// <remarks>
        /// If not, this is an authoritative deletion from the owner to a proxy.
        /// </remarks>
        public bool IsRequest { get; set; }

        public ReliableMessage()
        { }

        public ReliableMessage(int id, bool isRequest) : base(id)
        {
            IsRequest = isRequest;
        }
    }
}
