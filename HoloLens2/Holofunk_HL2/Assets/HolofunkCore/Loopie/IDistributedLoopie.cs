// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Loopie
{
    /// <summary>
    /// The distributed interface of a loopie.
    /// </summary>
    /// <remarks>
    /// We currently have loopies owned by the loopie's creator. However, if the creator disconnects,
    /// their loopies will all vanish. It may be that this actually is a worse experience, and perhaps
    /// all the loopies should be owned by the viewpoint that's doing the sound rendering. For now, we
    /// go with creator-owned loopies.
    /// </remarks>
    public interface IDistributedLoopie : IDistributedInterface
    {
        /// <summary>
        /// The state of the Loopie.
        /// </summary>
        Loopie GetLoopie();

        /// <summary>
        /// Mute or unmute the Loopie.
        /// </summary>
        void SetMute(bool mute);

        /// <summary>
        /// Set the current volume of this Loopie.
        /// </summary>
        /// <remarks>
        /// This should be a broadcast message, and eventually probably will be, but at first we
        /// make it reliable.
        /// </remarks>
        void SetVolume(float volume);
    }
}
