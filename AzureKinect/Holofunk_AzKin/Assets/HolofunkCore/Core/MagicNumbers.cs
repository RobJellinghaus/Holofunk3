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
        public static readonly int FramesToAverageWhenSmoothing = 10;

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
        public static readonly float HandRadius = 0.20f; // 20 cm = 8 inches. Pretty big but let's start there

        /// <summary>
        /// The number of seconds before we decide that recognition of a player is lost and we give up.
        /// </summary>
        public static readonly ContinuousDuration<Second> RecognitionLossDuration = 5;

        #endregion

        #region Sound

        /// <summary>
        /// Number of seconds worth of sound to "pre-record" when starting a new track.
        /// </summary>
        /// <remarks>
        /// Functionally, this is latency compensation for the relatively slow and conservative gesture recognition.
        /// </remarks>
        public static readonly ContinuousDuration<Second> PreRecordingDuration = 0.1f;

        /// <summary>
        /// The wet/dry scale factor for changing the level of an effect.
        /// </summary>
        /// <remarks>
        /// The level widget's value range is from -1 to 1, and the effect level wet/dry scale is from 0 to 100.
        /// So this scale factor sets how much change gets applied to effect level by the greatest increase/decrease possible.
        /// </remarks>
        public static readonly float EffectLevelScale = 75f;

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
        /// 12 divisions per octave = 12 semitones per octave... how diatonic
        /// </summary>
        public static readonly int OctaveDivisions = 12;
        /// <summary>
        /// The central frequency of the histogram (Hz).
        /// </summary>
        /// <remarks>
        /// Low C (C3) = 130.81 Hz.
        /// Low G (G3) = 196.00 Hz.
        /// Middle C (C4) = 261.626 Hz.
        /// Middle G (G4) = 391.995 Hz.
        /// High C (C5) = 532.25 Hz.
        /// High G (G5) = 783.99 Hz.
        /// </remarks>
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
        /// <remarks>
        /// The actual edge of the Azure Kinect body tracking is very small (pretty close to square in fact),
        /// so this is surprisingly high.
        /// </remarks>
        public static float MinDotProductForPanning = 0.8f;

        /// <summary>
        /// Minimum scale factor for loopies at minimum volume. (Max scale factor is always 1)
        /// </summary>
        /// <remarks>
        /// For frequency displays, this is the scale at minimum volume.
        /// </remarks>
        internal static float MinVolumeScale = 0.2f;

        /// <summary>
        /// Minimum loopie scale for minimum signal.
        /// </summary>
        public static readonly float MinLoopieScale = 1.5f;

        /// <summary>
        /// Maximum loopie scale for maximum signal.
        /// </summary>
        public static readonly float MaxLoopieScale = 2f;

        /// <summary>
        /// Loopie amplitude is logarithmic, so we boost it up to make it more noticeable.
        /// </summary>
        /// <remarks>
        /// TODO: consider emitting RMS or other linearized amplitude
        /// </remarks>
        public static readonly float LoopieSignalBias = 0.1f;

        /// <summary>
        /// Number of powers of 10 to multiply the average signal value by.
        /// </summary>
        /// <remarks>
        /// The signal value ranges from 0 to 1, so its logarithm is always negative;
        /// this adjustment sets the range of log10 values considered to be loud enough
        /// to alter the loopie's visual scale.
        /// </remarks>
        public static readonly float LoopieSignalExponent = 3;

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
        /// <remarks>
        /// This allows varying aspect ratios in the X/Z plane.
        /// </remarks>
        public static readonly float FrequencyDiscWidthScaleFactor = 1.2f;

        /// <summary>
        /// The scale of each frequency disc's depth, relative to the original scale of its shape.
        /// </summary>
        /// <remarks>
        /// This allows varying aspect ratios in the X/Z plane.
        /// </remarks>
        public static readonly float FrequencyDiscDepthScaleFactor = 0.8f;

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
        public static readonly float FrequencyBinMinValue = 5f;

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
        public static readonly float FrequencyShapeMinAlpha = 0.5f;

        /// <summary>
        /// Least transparent alpha value for maximal frequency shape.
        /// </summary>
        public static readonly float FrequencyShapeMaxAlpha = 1.0f;

        /// <summary>
        /// The ratio by which a scaled bin value should decay towards its expected lower value.
        /// </summary>
        public static readonly float BinValueDecay = 0.3f;

        #endregion

        #region Menus

        /// <summary>
        /// scale factor to apply to the distance of menu nodes
        /// </summary>
        public static readonly float MenuScale = 0.8f;

        #endregion

        #region Volume widgets

        /// <summary>
        /// Maximum amount louder the widget can louden (as a ratio of original volume).
        /// </summary>
        /// <remarks>
        /// MinRatio = 1 / MaxRatio
        /// </remarks>
        public static readonly float MaxVolumeRatio = 2;

        /// <summary>
        /// Maximum distance in meters to reach the MaxRatio volume.
        /// </summary>
        /// <remarks>
        /// The same distance downwards will soften by the MinRatio amount.
        /// </remarks>
        public static readonly float MaxVolumeHeightMeters = 0.1f; // 100 cm

        #endregion
    }
}
