// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using Holofunk.Viewpoint;
using NowSoundLib;
using System;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// The local implementation of a Loopie object.
    /// </summary>
    public class LocalLoopie : MonoBehaviour, IDistributedLoopie, ILocalObject
    {
        /// <summary>
        /// The state of this Loopie, in distributed terms.
        /// </summary>
        private Loopie loopie;

        /// <summary>
        /// If there is a SoundManager, and we created a track, this is its ID.
        /// </summary>
        /// <remarks>
        /// For now, we do not expose this as distributed state.
        /// </remarks>
        private TrackId trackId;

        /// <summary>
        /// The current amplitude; broadcast from one proxy to all others.
        /// </summary>
        private float minAmplitude, avgAmplitude, maxAmplitude;

        /// <summary>
        /// The highest timestamp received so far.
        /// </summary>
        /// <remarks>
        /// We don't worry about 64 bit overflow even at 48Khz timing rate.
        /// </remarks>
        private ulong maxTimestamp;

        private Vector3 lastViewpointPosition = Vector3.zero;

        internal void Initialize(Loopie loopie)
        {
            this.loopie = loopie;

            // and if there is a sound manager, start recording!
            if (SoundManager.Instance != null)
            {
                trackId = NowSoundGraphAPI.CreateRecordingTrackAsync(loopie.AudioInput.Value);
            }
        }

        #region MonoBehaviour

        public void Update()
        {
            // move to viewpoint position if viewpoint position moved (and/or viewpoint matrix came back)
            if (lastViewpointPosition != loopie.ViewpointPosition)
            {
                if (DistributedViewpoint.TheViewpoint != null)
                {
                    Matrix4x4 viewpointToLocalMatrix = DistributedViewpoint.TheViewpoint.ViewpointToLocalMatrix();
                    Vector3 localLoopiePosition = viewpointToLocalMatrix.MultiplyPoint(loopie.ViewpointPosition);
                    lastViewpointPosition = loopie.ViewpointPosition;

                    transform.localPosition = localLoopiePosition;
                }
            }

            if (SoundManager.Instance != null)
            {
                NowSoundSignalInfo signalInfo = NowSoundTrackAPI.SignalInfo(trackId);
                ((DistributedLoopie)DistributedObject).SetCurrentAmplitude(
                    signalInfo.Min, 
                    signalInfo.Avg, 
                    signalInfo.Max, 
                    (ulong)(long)Clock.Instance.AudioNow.Time);
            }

            if (avgAmplitude > 0f)
            {
                float delta = MagicNumbers.MaxLoopieScale - MagicNumbers.MinLoopieScale;
                float scale = MagicNumbers.MinLoopieScale + avgAmplitude * delta * MagicNumbers.LoopieAmplitudeBias;
                // and clamp in case bias sends us over
                scale = Math.Max(scale, MagicNumbers.MaxLoopieScale);
                transform.localScale = new Vector3(scale, scale, scale);
            }

            // get the color of the prototype loopie
            LocalLoopie prototypeLocalLoopie = DistributedObjectFactory.FindPrototypeComponent<LocalLoopie>(
                DistributedObjectFactory.DistributedType.Loopie);

            Color color = prototypeLocalLoopie.transform.GetChild(0).GetComponent<Renderer>().material.color;
            if (IsTouched)
            {
                color = new Color(Increase(color.r), Increase(color.g), Increase(color.b), Increase(color.a));
            }
            if (GetLoopie().IsMuted)
            {
                float average = (color.r + color.g + color.b) / 3;
                color = new Color(average, average, average, color.a);
            }

            transform.GetChild(0).GetComponent<Renderer>().material.color = color;
        }

        /// <summary>
        /// Increase value by 2/3 of its remaining distance from 1.
        /// </summary>
        private float Increase(float value)
        {
            Core.Contract.Requires(value >= 0 && value <= 1, "value >= 0 && value <= 1");

            return value + ((1f - value) * (2/3f));
        }

        #endregion

        #region IDistributedLoopie

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedLoopie>();

        /// <summary>
        /// Get the loopie's state.
        /// </summary>
        public Loopie GetLoopie() => loopie;

        public void OnDelete()
        {
            if (SoundManager.Instance != null)
            {
                Core.Contract.Assert(trackId != TrackId.Undefined);

                NowSoundGraphAPI.DeleteTrack(trackId);
            }
        }

        public void SetMute(bool isMuted)
        {
            HoloDebug.Log($"LocalLoopie.SetMute: id {DistributedObject.Id}, isMuted {isMuted}");
            loopie.IsMuted = isMuted;

            if (SoundManager.Instance != null)
            {
                NowSoundTrackAPI.SetIsMuted(trackId, isMuted);
            }
        }

        public void SetVolume(float volume)
        {
            loopie.Volume = volume;
        }

        public void SetViewpointPosition(Vector3 viewpointPosition)
        {
            loopie.ViewpointPosition = viewpointPosition;
        }

        public void FinishRecording()
        {
            if (SoundManager.Instance != null)
            {
                Core.Contract.Assert(trackId != TrackId.Undefined);

                NowSoundTrackAPI.FinishRecording(trackId);
            }
        }

        public void SetCurrentAmplitude(float min, float avg, float max, ulong timestamp)
        {
            // ignore out of order timestamps
            if (timestamp > maxTimestamp)
            {
                maxTimestamp = timestamp;
                minAmplitude = min;
                avgAmplitude = avg;
                maxAmplitude = max;
            }
        }

        #endregion

        #region Touch operations

        /// <summary>
        /// Is this loopie currently being touched by any performer?
        /// </summary>
        /// <remarks>
        /// This is not distributed state; it is purely local, and moreover is recomputed every frame.
        /// </remarks>
        public bool IsTouched { get; set; }

        #endregion
    }
}
