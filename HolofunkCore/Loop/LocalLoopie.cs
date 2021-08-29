﻿// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Shape;
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
        private LoopieState loopie;

        /// <summary>
        /// If there is a SoundManager, and we created a track, this is its ID.
        /// </summary>
        /// <remarks>
        /// For now, we do not expose this as distributed state.
        /// </remarks>
        private TrackId trackId;

        /// <summary>
        /// The current overall information about the signal (e.g. amplitude).
        /// </summary>
        private NowSoundSignalInfo signalInfo;

        /// <summary>
        /// The current overall information about the track.
        /// </summary>
        private NowSoundLib.TrackInfo trackInfo;

        /// <summary>
        /// The highest info timestamp received so far.
        /// </summary>
        /// <remarks>
        /// We don't worry about 64 bit overflow even at 48Khz timing rate.
        /// </remarks>
        private ulong maxInfoTimestamp;

        /// <summary>
        /// The highest waveform timestamp received so far.
        /// </summary>
        /// <remarks>
        /// We don't worry about 64 bit overflow even at 48Khz timing rate.
        /// </remarks>
        private ulong maxWaveformTimestamp;

        private Vector3 lastViewpointPosition = Vector3.zero;

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

        /// <summary>
        /// The last known TrackInfo about this track.
        /// </summary>
        private NowSoundLib.TrackInfo lastTrackInfo;

        /// <summary>
        /// The list of plugin instances, when on the node running the soundmanager.
        /// </summary>
        private List<PluginInstanceIndex> pluginInstances = new List<PluginInstanceIndex>();

        #endregion

        #region MonoBehaviour

        /// <summary>
        /// The transform that contains all our BeatMeasureControllers.
        /// </summary>
        private Transform BeatMeasureContainer => transform.GetChild(1);

        public void Start()
        {
            // if there is a sound manager, start recording!
            if (SoundManager.Instance != null)
            {
                trackId = NowSoundGraphAPI.CreateRecordingTrackAsync(loopie.AudioInput.Value);
            }

            // TODO: support creating loopie with effects!
            // TODO: support applying effects to performer! LIST OF EFFECTS ON PERFORMER!

            frequencyBins = new float[MagicNumbers.OutputBinCount];
            scaledBins = new float[MagicNumbers.OutputBinCount];

            InstantiateFrequencyBandShapes();

            BeatMeasureContainer.GetChild(0).GetComponent<BeatMeasureController>().localLoopie = this;
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

            // instantiate them into the "default" first measure controller, which is in the scene as an empty game object
            Transform firstBeatMeasureControllerObject = BeatMeasureContainer.GetChild(0);
            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                GameObject disc = ShapeContainer.InstantiateShape(ShapeType.Cylinder, firstBeatMeasureControllerObject);
                disc.SetActive(true);
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

        public void Update()
        {
            UpdateSoundData();

            UpdateHeldLoopiePosition();

            // Now that track info has been updated, update the controllers displaying what measure it is.
            UpdateMeasureControllers();

            // Now that the loopie underlying data is fully updated for this frame, update the loopie's appearance.
            UpdateLoopieAppearance();
        }

        /// <summary>
        /// 4/4 time right now. TODO: allow changing time signature
        /// </summary>
        private readonly static int BeatsPerMeasure = 4;

        /// <summary>
        /// Update (and possibly add to) the BeatMeasureControllers.
        /// </summary>
        private void UpdateMeasureControllers()
        {
            int measureControllerCount = BeatMeasureContainer.childCount;
            // Did we advance to a new measure?
            // If so, make a new BeatMeasureController and move the existing ones to the left.
            if ((int)trackInfo.DurationInBeats > measureControllerCount * BeatsPerMeasure)
            {
                // we need another beatMeasureController.
                BeatMeasureController lastBeatMeasureController = 
                    BeatMeasureContainer
                    .GetChild(measureControllerCount - 1)
                    .GetComponent<BeatMeasureController>();

                GameObject newBeatMeasureControllerGameObject = Instantiate(lastBeatMeasureController.gameObject, lastBeatMeasureController.transform.parent);
                newBeatMeasureControllerGameObject.name = $"BeatMeasureController#{transform.childCount}";
                BeatMeasureController newBeatMeasureController = newBeatMeasureControllerGameObject.GetComponent<BeatMeasureController>();
                newBeatMeasureController.startingMeasure = measureControllerCount;
                newBeatMeasureController.localLoopie = this;

                newBeatMeasureController.transform.localPosition = lastBeatMeasureController.transform.localPosition + new Vector3(MagicNumbers.BeatMeasureSeparation * 2, 0, 0);

                for (int i = 0; i < BeatMeasureContainer.childCount; i++)
                {
                    // shove them all to the left just a bit
                    BeatMeasureContainer.GetChild(i).localPosition -= new Vector3(MagicNumbers.BeatMeasureSeparation, 0, 0);
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
                if (DistributedViewpoint.Instance != null)
                {
                    Matrix4x4 viewpointToLocalMatrix = DistributedViewpoint.Instance.ViewpointToLocalMatrix();
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
                ulong timestamp = (ulong)(long)DistributedSoundClock.Instance.TimeInfo.Value.TimeInSamples;

                // we broadcast two timestamped packets per frame per loopie:
                // - the signal and track info
                // - the waveform info

                NowSoundSignalInfo signalInfo = NowSoundTrackAPI.SignalInfo(trackId);
                NowSoundLib.TrackInfo trackInfo = NowSoundTrackAPI.Info(trackId);
                ((DistributedLoopie)DistributedObject).SetCurrentInfo(
                    new SignalInfo(signalInfo),
                    new Sound.TrackInfo(trackInfo),
                    timestamp);

                NowSoundTrackAPI.GetFrequencies(trackId, frequencyBins);
                ((DistributedLoopie)DistributedObject).SetCurrentWaveform(
                    frequencyBins,
                    timestamp);
            }
        }

        /// <summary>
        /// Update the graphical state of this loopie as appropriate for the current frame.
        /// </summary>
        private void UpdateLoopieAppearance()
        {
            if (signalInfo.Avg > 0f)
            {
                float delta = MagicNumbers.MaxLoopieScale - MagicNumbers.MinLoopieScale;
                float scale = MagicNumbers.MinLoopieScale + signalInfo.Avg * delta * MagicNumbers.LoopieAmplitudeBias;
                // and clamp in case bias sends us over
                scale = Math.Max(scale, MagicNumbers.MaxLoopieScale);
                transform.localScale = new Vector3(scale, scale, scale);
            }

            // get the max value -- these can be anything, typically from <1 for almost inaudible up to 100 - 400 for quite loud.
            // Intensity values seem greater at lower frequencies.  So we just scale everything to the maximum in this histogram.
            // TODO: also scale by volume for total size?
            float maxFrequencyValue = 0;
            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                if (float.IsNaN(frequencyBins[i]))
                {
                    // We don't even try to deal with any NaNs, which evidently happen only when the loop is just starting.
                    return;
                }

                if (frequencyBins[i] < MagicNumbers.FrequencyBinMinValue)
                {
                    // ignore very tiny bins; very tiny max values will blow up when normalizing
                    continue;
                }

                maxFrequencyValue = Mathf.Max(maxFrequencyValue, frequencyBins[i]);
            }

            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                Color discColor = FrequencyBinColor(i);
                if (maxFrequencyValue == 0)
                {
                    Vector3 newLocalScale = new Vector3(
                        originalBandShapeLocalScale.x * MagicNumbers.MinVolumeScale,
                        originalBandShapeLocalScale.y,
                        originalBandShapeLocalScale.z * MagicNumbers.MinVolumeScale);

                    frequencyBandShapes[i].transform.localScale = newLocalScale;

                    discColor.a = MagicNumbers.FrequencyShapeMinAlpha;
                }
                else
                {
                    // TODO: fundamentally restructure ALL OF THIS to do proper RMS calculation or something

                    Core.Contract.Assert(!float.IsNaN(signalInfo.Avg));

                    // first, normalize to max
                    float normalizedValue = frequencyBins[i] / maxFrequencyValue;
                    Core.Contract.Assert(!float.IsNaN(normalizedValue));

                    // now, lerp multiplicatively to increase the size of small values
                    float boostedValue = normalizedValue * Mathf.Lerp(MagicNumbers.LerpVolumeScaleFactor, 1, normalizedValue);
                    Core.Contract.Assert(!float.IsNaN(boostedValue));

                    // now, multiply by some proportion of the volume
                    float volumeScaledValue = boostedValue * (signalInfo.Avg * Mathf.Lerp(MagicNumbers.LerpVolumeScaleFactor, 1, normalizedValue));
                    Core.Contract.Assert(!float.IsNaN(volumeScaledValue));

                    // now, lerp from a baseline value to ensure zero volume isn't invisible
                    float flooredValue = Mathf.Lerp(MagicNumbers.MinVolumeScale, 1, volumeScaledValue);
                    Core.Contract.Assert(!float.IsNaN(flooredValue));

                    // now, look up what the value was last time
                    float lastFlooredValue = scaledBins[i];
                    Core.Contract.Assert(!float.IsNaN(lastFlooredValue));

                    float decayedValue = lastFlooredValue - ((lastFlooredValue - flooredValue) * MagicNumbers.BinValueDecay);
                    Core.Contract.Assert(!float.IsNaN(decayedValue));

                    float targetValue = Mathf.Max(flooredValue, decayedValue);
                    Core.Contract.Assert(!float.IsNaN(targetValue));

                    Vector3 newLocalScale = new Vector3(
                        originalBandShapeLocalScale.x * targetValue,
                        originalBandShapeLocalScale.y,
                        originalBandShapeLocalScale.z * targetValue);
                    scaledBins[i] = targetValue;

                    frequencyBandShapes[i].transform.localScale = newLocalScale;

                    discColor.a = Mathf.Lerp(MagicNumbers.FrequencyShapeMinAlpha, MagicNumbers.FrequencyShapeMaxAlpha, boostedValue);
                }

                // now update the color.

                if (IsTouched)
                {
                    discColor = new Color(Increase(discColor.r), Increase(discColor.g), Increase(discColor.b), Increase(discColor.a));
                }
                if (GetLoopie().IsMuted)
                {
                    float average = (discColor.r + discColor.g + discColor.b) / 3;
                    discColor = new Color(average, average, average, discColor.a);
                }

                frequencyBandShapes[i].GetComponent<Renderer>().material.color = discColor;
            }
        }

        /// <summary>
        /// Increase value by 2/3 of its remaining distance from 1.
        /// </summary>
        private float Increase(float value)
        {
            Core.Contract.Requires(value >= 0 && value <= 1, "value >= 0 && value <= 1");

            return value + ((1f - value) * (2/3f));
        }

        public NowSoundLib.TrackInfo TrackInfo => trackInfo;

        #endregion

        #region IDistributedLoopie

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedLoopie>();

        /// <summary>
        /// Get the loopie's state.
        /// </summary>
        public LoopieState GetLoopie() => loopie;

        internal void Initialize(LoopieState loopie)
        {
            this.loopie = loopie;
        }

        public void OnDelete()
        {
            HoloDebug.Log($"LocalLoopie.OnDelete: Deleting {DistributedObject.Id}");
            if (SoundManager.Instance != null)
            {
                Core.Contract.Assert(trackId != TrackId.Undefined);

                NowSoundGraphAPI.DeleteTrack(trackId);
            }

            // and we blow ourselves awaaaay
            Destroy(gameObject);
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

        public void AppendSoundEffect(EffectId effect)
        {
            HoloDebug.Log($"LocalLoopie.AppendSoundEffect: id {DistributedObject.Id}, pluginId {effect.PluginId}, programId {effect.PluginProgramId}");
            int length = loopie.Effects == null ? 0 : loopie.Effects.Length / 2;
            int[] newEffects = new int[(length + 1) * 2];
            if (loopie.Effects != null)
            {
                loopie.Effects.CopyTo(newEffects, 0);
            }
            newEffects[length * 2] = (int)effect.PluginId.Value;
            newEffects[length * 2 + 1] = (int)effect.PluginId.Value;

            loopie.Effects = newEffects;

            if (SoundManager.Instance != null)
            {
                // add that plugin instance index!
                // Note that these are never sent over the network, as they're no good to any other node anyway.
                pluginInstances.Add(
                    NowSoundTrackAPI.AddPluginInstance(trackId, effect.PluginId.Value, effect.PluginProgramId.Value, 100));
            }
        }

        public void ClearSoundEffects()
        {
            HoloDebug.Log($"LocalLoopie.ClearSoundEffects: id {DistributedObject.Id}, {pluginInstances.Count} effects");
            loopie.Effects = new int[0];

            if (SoundManager.Instance != null)
            {
                // clean up all the plugins
                foreach (PluginInstanceIndex pluginInstance in pluginInstances)
                {
                    NowSoundTrackAPI.DeletePluginInstance(trackId, pluginInstance);
                }
                pluginInstances.Clear();
            }
        }

        public void SetCurrentInfo(SignalInfo signalInfo, Sound.TrackInfo trackInfo, ulong timestamp)
        {
            // ignore out of order timestamps
            if (timestamp > maxInfoTimestamp)
            {
                this.signalInfo = signalInfo.Value;
                this.trackInfo = trackInfo.Value;
                maxInfoTimestamp = timestamp;
            }
        }

        public void SetCurrentWaveform(float[] frequencyBins, ulong timestamp)
        {
            // ignore out of order timestamps
            if (timestamp > maxWaveformTimestamp)
            {
                maxWaveformTimestamp = timestamp;
                frequencyBins.CopyTo(this.frequencyBins, 0);
            }
        }

        #endregion

        #region Touch operations

        /// <summary>
        /// Is this loopie currently being touched by any performer?
        /// </summary>
        /// <remarks>
        /// This is not distributed state; it is purely local, and moreover is recomputed every frame.
        /// 
        /// This is specifically recomputed as a result of the script execution order. On every frame:
        /// - LoopiePreController sets IsTouched of all LocalLoopies to false.
        /// - PerformerPreController sets the performer's list of touched loopies to empty.
        /// - HandController (both of them) set their own internal list of touched loopies.
        /// - PerformerPostController collects the loopies touched by each hand and updates the touched loopie list.
        /// - LoopiePostController updates the IsTouched field of all loopies.
        /// 
        /// (Note that the distributed IsTouched state is updated by propagating the performer's
        /// touched loopie list. In other words, on the Azure Kinect side, there is only the LoopiePreController
        /// and LoopiePostController, and they work solely off the proxy Performer state which is updated via
        /// the network.)
        /// </remarks>
        public bool IsTouched { get; set; }

        #endregion
    }
}
