/// Copyright (c) 2021 by Rob Jellinghaus. All rights reserved.

namespace Holofunk.HandPose
{
    /// <summary>
    /// Which pose is each finger in?
    /// </summary>
    /// <remarks>
    /// Curled is as in making a fist; Extended is straight out; Unknown is anything in between.
    /// </remarks>
    public enum FingerPose
    {
        /// <summary>
        /// We don't know what pose this finger is in.
        /// </summary>,
        Unknown,

        /// <summary>
        /// We are pretty sure this finger is curled up (as when making a fist).
        /// </summary>
        Curled,

        /// <summary>
        /// We are pretty sure this finger is extended more or less straight out.
        /// </summary>
        Extended
    }
}
