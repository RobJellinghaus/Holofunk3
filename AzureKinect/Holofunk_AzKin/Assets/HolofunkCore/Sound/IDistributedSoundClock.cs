// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;

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
        /// The current time info for the clock.
        /// </summary>
        [LocalMethod]
        TimeInfo TimeInfo { get; }

        /// <summary>
        /// Update the time info as time passes.
        /// </summary>
        /// <remarks>
        /// TODO: should UpdateTimeInfo be a BroadcastMethod?
        /// </remarks>
        [ReliableMethod]
        void UpdateTimeInfo(TimeInfo timeInfo);

        /// <summary>
        /// Set the beats per minute going forward.
        /// </summary>
        [ReliableMethod]
        void SetBeatsPerMinute(float newBPM);
    }
}
