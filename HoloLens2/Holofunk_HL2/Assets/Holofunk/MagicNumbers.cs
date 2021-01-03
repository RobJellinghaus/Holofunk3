/// Copyright by Rob Jellinghaus.  All rights reserved.

namespace Holofunk
{
    /// <summary>
    /// Constants that are tuned manually for good subjective feel of the application.
    /// </summary>
    /// <remarks>
    /// These are deliberately mutable in case it becomes useful to tune some of them during execution.
    /// However, not all of these are safe to modify randomly during execution.
    /// </remarks>
    public static class MagicNumbers
    {
        /// <summary>
        /// The threshold value for finger linearity (as calculated by HandPoseRecognizer.CalculateFingerPose), above which
        /// the finger is considered extended.
        /// </summary>
        public static float FingerLinearityExtendedMinimum = 2.5f;

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
        /// The minimum ratio of distance between the base joint pair and the end joint pair for a pair of fingers to
        /// be considered non-adjacent.
        /// </summary>
        public static float FingerNonAdjacencyMinimum = 1.5f;

        /// <summary>
        /// The maximum ratio of distance between the base joint pair and the end joint pair for a pair of fingers to
        /// be considered adjacent.
        /// </summary>
        public static float FingerAdjacencyMaximum = 1.2f;
    }
}
