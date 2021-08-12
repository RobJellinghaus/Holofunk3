// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using Holofunk.Viewpoint;
using NowSoundLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Loop
{
    /// <summary>
    /// The local implementation of a Loopie object.
    /// </summary>
    public class LocalLoopie : MonoBehaviour, IDistributedLoopie, ILocalObject
    {
        #region Fields

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
        /// The highest amplitude timestamp received so far.
        /// </summary>
        /// <remarks>
        /// We don't worry about 64 bit overflow even at 48Khz timing rate.
        /// </remarks>
        private ulong maxAmplitudeTimestamp;

        /// <summary>
        /// The highest waveform timestamp received so far.
        /// </summary>
        /// <remarks>
        /// We don't worry about 64 bit overflow even at 48Khz timing rate.
        /// </remarks>
        private ulong maxWaveformTimestamp;

        private Vector3 lastViewpointPosition = Vector3.zero;

        /// <summary>
        /// The controllers displaying the various measures of our track's duration.
        /// </summary>
        /// <remarks>
        /// If, for instance, we have a 14-beat track in with a 4-beats-per-measure Clock, there will be four BeatMeasureControllers;
        /// the first three will be displaying four beats each, and the final one only two.
        /// </remarks>
        private List<BeatMeasureController> beatMeasureControllers;

        /// <summary>
        /// The list of shapes, one per frequency band, for this loopie's display.
        /// </summary>
        private List<GameObject> frequencyBandShapes;

        /// <summary>
        /// The storage for the frequency data for this loopie.
        /// </summary>
        private float[] frequencyBins;

        /// <summary>
        /// The scaled bins that we displayed last, for decay/hysteresis.
        /// </summary>
        private float[] scaledBins;

        /// <summary>
        /// The original local scale of each frequency band shape (they all start out with the same local scale).
        /// </summary>
        private Vector3 originalBandShapeLocalScale;

        #endregion

        #region MonoBehaviour

        public void Start()
        {
            // if there is a sound manager, start recording!
            if (SoundManager.Instance != null)
            {
                trackId = NowSoundGraphAPI.CreateRecordingTrackAsync(loopie.AudioInput.Value);
            }

            frequencyBins = new float[MagicNumbers.OutputBinCount];
            scaledBins = new float[MagicNumbers.OutputBinCount];

            InstantiateFrequencyBandShapes();

            beatMeasureControllers = new List<BeatMeasureController>();
            beatMeasureControllers.Add(transform.GetChild(1).GetComponent<BeatMeasureController>());
            beatMeasureControllers[0].audioTrack = trackId;
        }

        private void InstantiateFrequencyBandShapes()
        {
            // who needs reallocation when we know the capacity
            frequencyBandShapes = new List<GameObject>(MagicNumbers.OutputBinCount);

            // The goal here is to instantiate a literal (e.g. visual, in-world) stack of MagicNumbers.OutputBinCount
            // short cylinders.  Each cylinder's radius will animate and colorize based on an associated frequency band.
            // So we want to add all these as children of our transform.
            // We go from lowest frequency band (bottom of the stack) to highest (top).
            // Likewise we colorize with red hue on the bottom and violet hue on the top.
            // So audio frequency, visual color, and in-world height in the stack all correlate.
            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                GameObject disc = InstantiateShape(Shape.Cylinder.ToString());
                disc.transform.localPosition = new Vector3(0, i * MagicNumbers.FrequencyDiscVerticalDistance, 0);
                Vector3 localScale = disc.transform.localScale;
                // TODO: magic constant for x/z (e.g. width, since x/z symmetrical) scale
                disc.transform.localScale = new Vector3(
                    localScale.x * MagicNumbers.FrequencyDiscWidthScaleFactor,
                    localScale.y * MagicNumbers.FrequencyDiscHeightScaleFactor,
                    localScale.z * MagicNumbers.FrequencyDiscWidthScaleFactor);

                // a bit wasteful to do redundantly per shape, but simplifies the logic since we don't have to go look at the prototype
                originalBandShapeLocalScale = disc.transform.localScale;

                // Set the disc's color based on HSV distance!
                Color discColor = FrequencyBinColor(i);
                discColor.a = MagicNumbers.FrequencyShapeMinAlpha;
                disc.GetComponent<Renderer>().material.color = discColor;

                frequencyBandShapes.Add(disc);
            }
        }

        private static Color FrequencyBinColor(int frequencyBinIndex)
        {
            return Color.HSVToRGB(
                Mathf.Lerp(
                    MagicNumbers.FrequencyShapeMinHue,
                    MagicNumbers.FrequencyShapeMaxHue,
                    ((float)frequencyBinIndex) / MagicNumbers.OutputBinCount),
                1,
                1);
        }

        private GameObject InstantiateShape(string shapeName)
        {
            // get the ShapeContainer
            GameObject shapeContainer = GameObject.Find("ShapeContainer");
            GameObject prototypeShape = shapeContainer.transform.Find(shapeName).gameObject;

            Core.Contract.Assert(prototypeShape != null);

            // clone the shape and put the clone at this Loopie's position
            GameObject shape = Instantiate(prototypeShape, transform.GetChild(0)); // child 0 is the ShapeContainer for this Loopie
            shape.transform.localPosition = Vector3.zero;
            return shape;
        }

        public void Update()
        {
            // Update the controllers displaying what measure it is.
            UpdateMeasureControllers();

            UpdateHeldLoopiePosition();

            UpdateSoundData();

            UpdateLoopieAppearance();
        }

        /// <summary>
        /// Update (and possibly add to) the BeatMeasureControllers.
        /// </summary>
        private void UpdateMeasureControllers()
        {
            TrackInfo trackTimeInfo = NowSoundTrackAPI.Info(trackId);

            // Did we advance to a new measure?
            // If so, make a new BeatMeasureController and move the existing ones to the left.
            if ((int)trackTimeInfo.DurationInBeats > beatMeasureControllers.Count * Clock.Instance.BeatsPerMeasure)
            {
                // we need another beatMeasureController.
                BeatMeasureController lastBeatMeasureController = beatMeasureControllers[beatMeasureControllers.Count - 1];
                GameObject newBeatMeasureControllerGameObject = Instantiate(lastBeatMeasureController.gameObject, transform);
                newBeatMeasureControllerGameObject.name = $"BeatMeasureController#{++s_beatMeasureControllerTotalCount}";
                BeatMeasureController newBeatMeasureController = newBeatMeasureControllerGameObject.GetComponent<BeatMeasureController>();
                newBeatMeasureController.startingMeasure = beatMeasureControllers.Count;
                newBeatMeasureController.audioTrack = trackId;

                beatMeasureControllers.Add(newBeatMeasureController);

                newBeatMeasureController.transform.localPosition = lastBeatMeasureController.transform.localPosition + new Vector3(MagicNumbers.BeatMeasureSeparation * 2, 0, 0);

                for (int i = 0; i < beatMeasureControllers.Count; i++)
                {
                    // shove them all to the left just a bit
                    beatMeasureControllers[i].transform.localPosition -= new Vector3(MagicNumbers.BeatMeasureSeparation, 0, 0);
                }
            }
        }

        /// <summary>
        /// If we are holding the loopie, drag it around appropriately.
        /// </summary>
        private void UpdateHeldLoopiePosition()
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
        }

        /// <summary>
        /// If there is a sound manager, update the sound information (amplitude and waveform) for this loopie.
        /// </summary>
        private void UpdateSoundData()
        {
            if (SoundManager.Instance != null)
            {
                NowSoundSignalInfo signalInfo = NowSoundTrackAPI.SignalInfo(trackId);
                ((DistributedLoopie)DistributedObject).SetCurrentAmplitude(
                    signalInfo.Min,
                    signalInfo.Avg,
                    signalInfo.Max,
                    (ulong)(long)Clock.Instance.AudioNow.Time);
            }
        }

        /// <summary>
        /// Update the graphical state of this loopie as appropriate for the current frame.
        /// </summary>
        private void UpdateLoopieAppearance()
        {
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

        internal void Initialize(Loopie loopie)
        {
            this.loopie = loopie;
        }

        public void OnDelete()
        {
            if (SoundManager.Instance != null)
            {
                Core.Contract.Assert(trackId != TrackId.Undefined);

                NowSoundGraphAPI.DeleteTrack(trackId);
            }

            // and we blow ourselves awaaaay
            Destroy(this.gameObject);
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
            if (timestamp > maxAmplitudeTimestamp)
            {
                maxAmplitudeTimestamp = timestamp;
                minAmplitude = min;
                avgAmplitude = avg;
                maxAmplitude = max;
            }
        }

        public void SetCurrentWaveform(float[] buckets, ulong timestamp)
        {
            // ignore out of order timestamps
            if (timestamp > maxWaveformTimestamp)
            {
                maxWaveformTimestamp = timestamp;
                Buckets = buckets;
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
