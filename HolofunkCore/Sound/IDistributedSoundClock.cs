// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;

namespace Holofunk.Sound
{
    /// <summary>
    /// Read-only distributed object that provides state about the current audio time.
    /// </summary>
    /// <remarks>
    /// If there is a SoundManager, it will own one of these.
    /// </remarks>
    public interface IDistributedSoundClock : IDistributedInterface
    {
        /// <summary>
        /// The time info for the clock.
        /// </summary>
        public TimeInfo TimeInfo { get; }
    }
}
