﻿// Copyright by Rob Jellinghaus. All rights reserved.

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
        /// Alter the volume by the given amount.
        /// </summary>
        /// <remarks>
        /// As long as commit is false, the alteration is added to the current volume level and then clamped.
        /// Once commit is true, the current volume level is updated to the altered, clamped volume.
        /// </remarks>
        [ReliableMethod]
        void AlterVolume(float alteration, bool commit);

        /// <summary>
        /// Alter this sound effect by the given wet/dry amount.
        /// </summary>
        /// <remarks>
        /// If this effect did not exist on the loopie yet, add it with the initialLevel level.
        /// 
        /// As long as commit is false, the alteration is added to the current volume level and then clamped.
        /// Once commit is true, the current volume level is updated to the altered, clamped volume.
        /// </remarks>
        [ReliableMethod]
        void AlterSoundEffect(EffectId effect, int initialLevel, int levelAlteration, bool commit);

        /// <summary>
        /// Pop the most recently applied sound effect off of this track.
        /// </summary>
        [ReliableMethod]
        void PopSoundEffect();

        /// <summary>
        /// Clear all the sound effects on this track.
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
