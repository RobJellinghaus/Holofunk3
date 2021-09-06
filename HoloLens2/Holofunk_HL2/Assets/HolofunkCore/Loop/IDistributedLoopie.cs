// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Sound;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// The distributed interface of a loopie.
    /// </summary>
    /// <remarks>
    /// We currently have loopies owned by the loopie's creator. However, if the creator disconnects,
    /// their loopies will all vanish. It may be that this actually is a worse experience, and perhaps
    /// all the loopies should be owned by the viewpoint that's doing the sound rendering. For now, we
    /// go with creator-owned loopies.
    /// 
    /// All loopies are created in recording state, and need FinishRecording() to be called before they
    /// start looping (which they do as soon as appropriate after being told to finish recording).
    /// </remarks>
    public interface IDistributedLoopie : IDistributedInterface
    {
        /// <summary>
        /// The state of the Loopie.
        /// </summary>
        [LocalMethod]
        LoopieState GetLoopie();

        /// <summary>
        /// Move the loopie in space.
        /// </summary>
        /// <param name="viewpointPosition">The new position in viewpoint coordinates.</param>
        [ReliableMethod]
        void SetViewpointPosition(Vector3 viewpointPosition);

        /// <summary>
        /// Stop recording at the next quantized interval (as configured in NowSoundLib).
        /// </summary>
        [ReliableMethod]
        void FinishRecording();

        /// <summary>
        /// Mute or unmute the Loopie.
        /// </summary>
        [ReliableMethod]
        void SetMute(bool mute);

        /// <summary>
        /// Multiply the current volume of this loopie by this factor.
        /// </summary>
        /// <remarks>
        /// This is better than a straight SetVolume method, as it avoids issues with multiple
        /// users racing to raise/lower the volume (if one is raising and one is lowering,
        /// their effects will cancel out nicely with this method, as opposed to creating horrible
        /// rapid volume thrashing with a direct SetVolume).
        /// 
        /// The constraints on the ratio are intended to prevent muting something to zero and
        /// being unable to ever raise its volume again. Note that NowSoundLib clamps over-volume
        /// multiplications to a max amplitude of 1, which will clip and be terrible but will at
        /// least not break the sound driver.
        /// </remarks>
        /// <param name="ratio">The amount to multiply the volume by; must be between 0.1 and 10</param>
        [ReliableMethod]
        void MultiplyVolume(float ratio);

        /// <summary>
        /// Append this sound effect to the list of effects on this track.
        /// </summary>
        [ReliableMethod]
        void AppendSoundEffect(EffectId effect);

        /// <summary>
        /// Clear the sound effects on this track.
        /// </summary>
        [ReliableMethod]
        void ClearSoundEffects();

        /// <summary>
        /// Broadcast the current amplitude of this Loopie.
        /// </summary>
        [BroadcastMethod]
        void SetCurrentInfo(SignalInfo signalInfoPacket, TrackInfo trackInfoPacket, ulong timestamp);

        /// <summary>
        /// Broadcast the current waveform of this Loopie.
        /// </summary>
        [BroadcastMethod]
        void SetCurrentWaveform(float[] frequencyBins, ulong timestamp);
    }
}
