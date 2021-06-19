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
        public const int FramesToAverageWhenSmoothing = 10;
    }
}
