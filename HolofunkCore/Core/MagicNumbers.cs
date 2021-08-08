// Copyright by Rob Jellinghaus. All rights reserved.

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
        public static readonly float MinimumHeadViewpointAlignment = 0.90f;

        /// <summary>
        /// 2048 is enough to resolve down to about two octaves below middle C (e.g. 65 Hz).
        /// </summary>
        public static readonly int FftBinSize = 2048;
        /// <summary>
        /// Number of output bins; this can be whatever we want to see, rendering-wise.
        /// </summary>
        public static readonly int OutputBinCount = 20;
        /// <summary>
        /// Number of divisions per octave (e.g. setting this to 3 equals four semitones per bin, 12 divided by 3).
        /// </summary>
        public static readonly int OctaveDivisions = 5;
        /// <summary>
        /// The central frequency of the histogram; this is middle C.
        /// </summary>
        public static readonly float CentralFrequency = 261.626f;
        /// <summary>
        /// The bin (out of OutputBinCount) in which the central frequency should be mapped; zero-indexed.
        /// </summary>
        public static readonly int CentralFrequencyBin = 10;

        /// <summary>
        /// Minimum loopie scale for minimum amplitude.
        /// </summary>
        public static readonly float MinLoopieScale = 0.6f;

        /// <summary>
        /// Maximum loopie scale for maximum amplitude.
        /// </summary>
        public static readonly float MaxLoopieScale = 1.1f;

        /// <summary>
        /// Loopie amplitude is logarithmic, so we boost it up to make it more noticeable.
        /// </summary>
        /// <remarks>
        /// TODO: consider emitting RMS or other linearized amplitude
        /// </remarks>
        public static readonly float LoopieAmplitudeBias = 10.0f;
    }
}
