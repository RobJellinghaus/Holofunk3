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
        public static float FingerLinearityExtendedMinimum = 1.7f;

        /// <summary>
        /// The threshold value for finger linearity (as calculated by HandPoseRecognizer.CalculateFingerPose), below which
        /// the finger is considered curled.
        /// </summary>
        public static float FingerLinearityCurledMaximum = 0.8f;
    }
}
