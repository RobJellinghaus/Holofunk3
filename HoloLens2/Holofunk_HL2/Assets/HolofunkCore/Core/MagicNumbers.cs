// Copyright by Rob Jellinghaus. All rights reserved.

namespace Holofunk.Core
{
    /// <summary>
    /// Constants that are determined by hand-tuning or otherwise outside of Holofunk.
    /// </summary>
    public static class MagicNumbers
    {
        #region Motion sensing

        /// <summary>
        /// Number of frames over which to average, when smoothing positions and hand poses.
        /// </summary>
        public static readonly int FramesToAverageWhenSmoothing = 20;

        /// <summary>
        /// The minimum dot product between the head's forward direction and the current head-to-sensor ray.
        /// </summary>
        /// <remarks>
        /// In practice when looking right at the sensor the collinearity is over 0.99, so 0.90 is fairly safe.
        /// </remarks>
        public static readonly float MinimumHeadViewpointAlignment = 0.90f;

        /// <summary>
        /// The radius of the hand, in meters.
        /// </summary>
        public static readonly float HandRadius = 0.15f; // 15 cm = 6 inches. Pretty big but let's start there

        #endregion

        #region FFT

        /// <summary>
        /// 2048 is enough to resolve down to about two octaves below middle C (e.g. 65 Hz).
        /// </summary>
        public static readonly int FftBinSize = 2048;
        /// <summary>
        /// Number of output bins; this can be whatever we want to see, rendering-wise.
        /// </summary>
        public static readonly int OutputBinCount = 64;
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
        public static readonly int CentralFrequencyBin = 32;

        #endregion

        #region Loopies

        /// <summary>
        /// Minimum dot product between the sensor->head ray and the sensor forward direction, to be considered
        /// panned all the way to one side.
        /// </summary>
        public static float MinDotProductForPanning = 0.7f;

        /// <summary>
        /// Minimum scale factor for loopies at minimum volume. (Max scale factor is always 1)
        /// </summary>
        /// <remarks>
        /// For frequency displays, this is the scale at minimum volume.
        /// </remarks>
        internal static float MinVolumeScale = 0.2f;

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
        public static readonly float LoopieAmplitudeBias = 1.0f;

        /// <summary>
        /// The horizontal separation between measure indicators.
        /// </summary>
        public static readonly float BeatMeasureSeparation = 0.015f;

        /// <summary>
        /// The scale of each frequency disc's height, relative to the original scale of its shape.
        /// </summary>
        public static readonly float FrequencyDiscHeightScaleFactor = 0.05f;

        /// <summary>
        /// The scale of each frequency disc's width, relative to the original scale of its shape.
        /// </summary>
        public static readonly float FrequencyDiscWidthScaleFactor = 1f;

        /// <summary>
        /// The vertical distance apart to place each frequency disc.
        /// </summary>
        public static readonly float FrequencyDiscVerticalDistance = 0.002f;

        /// <summary>
        /// The minimum value below which frequency bins will be ignored.
        /// </summary>
        /// <remarks>
        /// Since frequency intensity values swing widely between frequency bands, and lower frequencies tend to have higher
        /// intensity values even at what seems like the same subjective volume, it's challenging to correlate absolute
        /// frequency intensity values to absolute visual scale or even subjective volume.  But heuristically we want to
        /// avoid "twitching" at volumes that seem quite low but that produce high variance.  So we clamp values lower than
        /// this to zero, as a crude low-pass filter.
        /// </remarks>
        public static readonly float FrequencyBinMinValue = 1f;

        /// <summary>
        /// The minimum hue value to use for the frequency shapes.
        /// </summary>
        public static readonly float FrequencyShapeMinHue = 0;

        /// <summary>
        /// The maximum hue value to use.
        /// </summary>
        public static readonly float FrequencyShapeMaxHue = 0.8f;

        /// <summary>
        /// Most transparent alpha value for minimal frequency shape.
        /// </summary>
        public static readonly float FrequencyShapeMinAlpha = 0.1f;

        /// <summary>
        /// Least transparent alpha value for maximal frequency shape.
        /// </summary>
        public static readonly float FrequencyShapeMaxAlpha = 0.9f;

        /// <summary>
        /// The ratio by which a scaled bin value should decay towards its expected lower value.
        /// </summary>
        public static readonly float BinValueDecay = 0.3f;

        #endregion

        #region Menus

        /// <summary>
        /// scale factor to apply to the distance of menu nodes
        /// </summary>
        public static readonly float MenuScale = 0.4f;

        #endregion

        #region Volume widgets

        /// <summary>
        /// Maximum amount louder the widget can louden (as a ratio of original volume).
        /// </summary>
        /// <remarks>
        /// MinRatio = 1 / MaxRatio
        /// </remarks>
        public static readonly float MaxVolumeRatio = 4;

        /// <summary>
        /// Maximum distance in meters to reach the MaxRatio volume.
        /// </summary>
        /// <remarks>
        /// The same distance downwards will soften by the MinRatio amount.
        /// </remarks>
        public static readonly float MaxVolumeHeightMeters = 0.03f; // 30 cm

        #endregion
    }
}
