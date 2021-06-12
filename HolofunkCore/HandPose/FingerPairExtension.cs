/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

namespace Holofunk.HandPose
{
    /// <summary>
    /// For each pair of fingers, how extended and adjacent are they?
    /// </summary>
    /// <remarks>
    /// This is calculated by determining how colinear the fingers are; if two adjacent fingers
    /// are highly colinear, they're guaranteed to be pointing in the same direction, hence together.
    /// </remarks>
    public enum FingerPairExtension
    {
        /// <summary>
        /// We don't know how close this pair of fingers are.
        /// </summary>
        Unknown,

        /// <summary>
        /// We are pretty confident these two fingers are extended side by side.
        /// </summary>
        ExtendedTogether,

        /// <summary>
        /// We are pretty confident these two fingers are NOT extended side by side.
        /// </summary>
        NotExtendedTogether
    }
}