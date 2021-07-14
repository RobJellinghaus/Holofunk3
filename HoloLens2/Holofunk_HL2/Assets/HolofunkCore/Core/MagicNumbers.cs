/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

namespace Holofunk.Core
{
    /// <summary>
    /// Constants that are determined by hand-tuning or otherwise outside of Holofunk.
    /// </summary>
    public static class MagicNumbers
    {
        /// <summary>
        /// Number of frames over which to average, when smoothing positions.
        /// </summary>
        public static readonly int FramesToAverageWhenSmoothing = 10;

        /// <summary>
        /// The minimum dot product between the head's forward direction and the current head-to-sensor ray.
        /// </summary>
        /// <remarks>
        /// In practice when looking right at the sensor the collinearity is over 0.99, so 0.90 is fairly safe.
        /// </remarks>
        public static readonly float MinimumGazeViewpointAlignment = 0.90f;
    }
}
