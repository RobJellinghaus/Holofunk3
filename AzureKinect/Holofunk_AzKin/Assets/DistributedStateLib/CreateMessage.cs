// Copyright (c) 2020 by Rob Jellinghaus.

namespace Distributed.State
{
    /// <summary>
    /// Message sent to create new proxy objects.
    /// </summary>
    /// <remarks>
    /// Derived classes include the initial state of the proxy object they create.
    /// 
    /// Note that derived classes get their own subscriptions; those subscriptions create
    /// proxy objects when CreateMessages are received.
    /// </remarks>
    public abstract class CreateMessage : ReliableMessage
    {
        public CreateMessage()
        { }

        public CreateMessage(DistributedId id) : base(id, isRequest: false)
        { }

        public override void Invoke(IDistributedInterface target)
        {
            // Create message is a special case; it doesn't invoke anything on the local object beyond constructing it.
        }
    }
}
