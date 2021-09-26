// Copyright by Rob Jellinghaus. All rights reserved.

namespace Holofunk.Hand
{
    /// <summary>
    /// Constants that are tuned manually for good subjective feel of the application.
    /// </summary>
    /// <remarks>
    /// These are deliberately mutable in case it becomes useful to tune some of them during execution.
    /// However, not all of these are safe to modify randomly during execution.
    /// </remarks>
    public static class HandPoseMagicNumbers
    {
        /// <summary>
        /// The threshold value for finger linearity (as calculated by HandPoseRecognizer.CalculateFingerPose), above which
        /// the finger is considered extended.
        /// </summary>
        public static float FingerLinearityExtendedMinimum = 2f;

        /// <summary>
        /// The threshold value for finger linearity (as calculated by HandPoseRecognizer.CalculateFingerPose), below which
        /// the finger is considered curled.
        /// </summary>
        public static float FingerLinearityCurledMaximum = 1.2f;

        /// <summary>
        /// The threshold value for thumb linearity (as calculated by HandPoseRecognizer.CalculateFingerPose), above which
        /// the finger is considered extended.
        /// </summary>
        public static float ThumbLinearityExtendedMinimum = 0.9f;

        /// <summary>
        /// The threshold value for thumb linearity (as calculated by HandPoseRecognizer.CalculateFingerPose), below which
        /// the thumb is considered curled.
        /// </summary>
        public static float ThumbLinearityCurledMaximum = 0.8f;

        /// <summary>
        /// The minimum upness of the thumb-proximal-to-tip vector for the gesture to be considered ThumbsUp.
        /// </summary>
        public static float ThumbVectorDotUpMinimum = 0.7f;

        /// <summary>
        /// The minimum ratio of distance between the base joint pair and the end joint pair for a pair of fingers to
        /// be considered non-adjacent.
        /// </summary>
        public static float FingerNonAdjacencyMinimum = 1.5f;

        /// <summary>
        /// The minimum colinearity between adjacent fingers to consider them extended together.
        /// </summary>
        /// <remarks>
        /// This seems high but "open" fingers still have colinearity of around 2.2.
        /// </remarks>
        public static float FingersExtendedColinearityMinimum = 0.975f;

        /// <summary>
        /// The maximum colinearity between adjacent fingers to consider them NOT extended together.
        /// </summary>
        public static float FingersNotExtendedColinearityMaximum = 0.95f;

        /// <summary>
        /// Minimum colinearity value to consider the finger aligned with the eye (e.g. on the other side of the palm from the eye).
        /// </summary>
        public static float FingerEyeColinearityMinimum = 0.7f;

        /// <summary>
        /// For detecting the 'bloom' gesture (all fingers together, above palm).
        /// </summary>
        public static float FingertipSumDistanceToKnuckleSumDistanceRatioMaximum = 0.75f;

        /// <summary>
        /// The required thumb tip altitude (above the proximal joint) to be considered "thumbs up".
        /// </summary>
        /// <remarks>TODO: make this be hand relative, not world space (for more robustness with smaller hands etc)</remarks>
        public static readonly float ThumbTipAltitude = 0.02f; // 2 cm

        /// <summary>The minimum ratio of the X/Z axis hand coordinate spread to the Y axis hand coordinate spread</summary>
        /// <remarks>In other words, the max/min X/Z hand joint coordinates need to be this much further apart than the
        /// min/max Y hand joint coordinates</remarks>
        public static readonly float HandFlatnessFactor = 2f;
    }
}
