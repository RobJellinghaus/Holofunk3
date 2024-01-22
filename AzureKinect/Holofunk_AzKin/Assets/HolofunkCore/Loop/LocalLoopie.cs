// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Core;
using Holofunk.Distributed;
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
        /// Is someone touching this loopie?
        /// </summary>
        private bool isTouched;

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
        /// The list of plugin instances, when on the node running the soundmanager.
        /// </summary>
        private List<PluginInstanceIndex> pluginInstances = new List<PluginInstanceIndex>();

        /// <summary>
        /// The last fractional beat value.
        /// </summary>
        /// <remarks>If this drops, and the current integer beat value is 0, then this loopie just looped.</remarks>
        private float lastFractionalBeat;

        /// <summary>
        /// The total number of beats that this loopie has played.
        /// </summary>
        /// <remarks>
        /// This allows loops of only one or two beats to rotate smoothly through their full circle.
        /// </remarks>
        private Duration<Beat> completedLoopBeats;

        #endregion

        #region MonoBehaviour

        /// <summary>
        /// The transform that contains all our BeatMeasureControllers.
        /// </summary>
        private Transform BeatMeasureContainer => transform.GetChild(1);

        public void Start()
        {
            // if there is a sound manager, let's set up audio.
            if (SoundManager.Instance != null)
            {
                // if we have an audio input, start recording!
                if (loopie.AudioInput.IsInitialized)
                {
                    trackId = NowSoundGraphAPI.CreateRecordingTrackAsync(loopie.AudioInput.Value);
                }
                else
                {
                    HoloDebug.Assert(loopie.CopiedLoopieId.IsInitialized, "Audio input wasn't defined so copied loopie ID should have been");

                    // look up the local loopie with that distributed ID
                    foreach (DistributedLoopie candidate in DistributedObjectFactory.FindComponentInstances<DistributedLoopie>(
                            DistributedObjectFactory.DistributedType.Loopie, false))
                    {
                        if (candidate.Id == loopie.CopiedLoopieId)
                        {
                            TrackId copiedTrackId = ((LocalLoopie)candidate.LocalObject).trackId;
                            HoloDebug.Log($"Found candidate with copied ID {loopie.CopiedLoopieId} and track ID {copiedTrackId}");

                            trackId = NowSoundGraphAPI.CopyLoopingTrack(copiedTrackId);
                        }
                    }
                }

                // Set up all the effects on this loopie right now.
                // While the loopie is recording, no sound will be played anyway; once the loopie
                // finishes recording, all the effects will kick in.
                for (int i = 0; i < loopie.EffectLevels.Length; i++)
                {
                    NowSoundTrackAPI.AddPluginInstance(
                        trackId,
                        (NowSoundLib.PluginId)loopie.Effects[i * 2],
                        (ProgramId)loopie.Effects[i * 2 + 1],
                        loopie.EffectLevels[i]);
                }
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
            Transform childShapeContainer = transform.GetChild(0);
            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                // really a very flat sort of cube... not much of a cube at all really
                GameObject disc = ShapeContainer.InstantiateShape(ShapeType.FlatCylinder, childShapeContainer);
                disc.SetActive(true);
                disc.transform.localPosition = new Vector3(0, i * MagicNumbers.FrequencyDiscVerticalDistance, 0);
                Vector3 localScale = disc.transform.localScale;
                // TODO: magic constant for x/z (e.g. width, since x/z symmetrical) scale
                disc.transform.localScale = new Vector3(
                    localScale.x * MagicNumbers.FrequencyDiscWidthScaleFactor,
                    localScale.y * MagicNumbers.FrequencyDiscHeightScaleFactor,
                    localScale.z * MagicNumbers.FrequencyDiscDepthScaleFactor);

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

            UpdateLoopiePanPosition();

            // Now that track info has been updated, update the controllers displaying what measure it is.
            UpdateMeasureControllers();

            // Now that the loopie underlying data is fully updated for this frame, update the loopie's appearance.
            UpdateLoopieAppearance();
        }

        /// <summary>
        /// Update (and possibly add to) the BeatMeasureControllers.
        /// </summary>
        private void UpdateMeasureControllers()
        {
            int measureControllerCount = BeatMeasureContainer.childCount;
            // Did we advance to a new measure?
            // If so, make a new BeatMeasureController and move the existing ones to the left.
            if ((int)trackInfo.BeatDuration > measureControllerCount * trackInfo.BeatsPerMeasure)
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

                newBeatMeasureController.transform.localPosition = 
                    lastBeatMeasureController.transform.localPosition 
                    + new Vector3(MagicNumbers.BeatMeasureSeparation * 2, 0, 0);

                for (int i = 0; i < BeatMeasureContainer.childCount; i++)
                {
                    // shove them all to the left just a bit
                    BeatMeasureContainer.GetChild(i).localPosition -= new Vector3(MagicNumbers.BeatMeasureSeparation, 0, 0);
                }
            }
        }

        /// <summary>
        /// Update the loopie's stereo panning, based on its position relative to the viewpoint's forward direction.
        /// </summary>
        /// <remarks>
        /// If the viewpoint had ears, this is what it would hear.
        /// </remarks>
        private void UpdateLoopiePanPosition()
        {
            // move to viewpoint position if viewpoint position moved (and/or viewpoint matrix came back)
            if (lastViewpointPosition != loopie.ViewpointPosition)
            {
                Vector3 loopieViewpointPosition = loopie.ViewpointPosition;

                if (DistributedViewpoint.Instance != null)
                {
                    Matrix4x4 viewpointToLocalMatrix = DistributedViewpoint.Instance.ViewpointToLocalMatrix();
                    Vector3 localLoopiePosition = viewpointToLocalMatrix.MultiplyPoint(loopieViewpointPosition);
                    lastViewpointPosition = loopie.ViewpointPosition;

                    // why the heck are there apparently some NaNs creeping in here?!?!
                    if (float.IsNaN(localLoopiePosition.x) || float.IsNaN(localLoopiePosition.y) || float.IsNaN(localLoopiePosition.z))
                    {
                        // DEBUG LIKE MAD
                    }   
                    else
                    {
                        transform.localPosition = localLoopiePosition;

                        // if this is the sound manager, set the panning properly for the moving loopie
                        if (SoundManager.Instance != null)
                        {
                            PlayerState firstPlayer = DistributedViewpoint.Instance.GetPlayerByIndex(0);
                            if (firstPlayer.Tracked)
                            {
                                float panValue = CalculatePanValue(firstPlayer.SensorPosition, firstPlayer.SensorForwardDirection, loopie.ViewpointPosition, log: true);
                                NowSoundTrackAPI.SetPan(trackId, panValue);
                                float updatedPan = NowSoundTrackAPI.Pan(trackId);
                                // HoloDebug.Log($"LocalLoopie.UpdateLoopiePanPosition: loopie {trackId}, viewpointPosition {loopie.ViewpointPosition}, panValue {panValue}, updatedPanValue {updatedPan}");
                            }
                        }
                    }
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
                // TODO: ugh, this is... not pretty. why do we have Yet Another Singleton? Eradicate the monoclock
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
                // signalInfo ranges from 0 to 1
                float delta = MagicNumbers.MaxLoopieScale - MagicNumbers.MinLoopieScale;
                float scale = MagicNumbers.MinLoopieScale
                    + (Mathf.Log10(signalInfo.Avg) + MagicNumbers.LoopieSignalExponent) * delta * MagicNumbers.LoopieSignalBias;
                // and clamp in case bias sends us over
                scale = Mathf.Min(Mathf.Max(scale, MagicNumbers.MaxLoopieScale), MagicNumbers.MinLoopieScale);
                transform.localScale = new Vector3(scale, scale, scale);
            }

            // get the max value -- these can be anything, typically from <1 for almost inaudible up to 100 - 400 for quite loud.
            // Intensity values seem greater at lower frequencies.  So we just scale everything to the maximum in this histogram.
            // TODO: also scale by volume for total size?
            float minFrequencyAmplitude = float.MaxValue;
            float maxFrequencyAmplitude = 0;
            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                if (float.IsNaN(frequencyBins[i]))
                {
                    // We don't even try to deal with any NaNs, which evidently happen only when the loop is just starting.
                    return;
                }

                if (frequencyBins[i] < MagicNumbers.FrequencyBinMinValue)
                {
                    // skip it, it's too quiet and will make it hard to see actual loudness
                    continue;
                }

                minFrequencyAmplitude = Mathf.Min(minFrequencyAmplitude, frequencyBins[i]);
                maxFrequencyAmplitude = Mathf.Max(maxFrequencyAmplitude, frequencyBins[i]);
            }

            if (maxFrequencyAmplitude == 0)
            {
                // there was no sound at all. set min to 0 also
                minFrequencyAmplitude = 0;
            }

            // calculate the top and bottom target values for stack rotation.
            // the way this works is:
            // - if the beat is even, we are rotating the top of stack by 90 degrees by end of beat;
            // - if the beat is odd, we are rotating the bottom of stack likewise.
            // All other rotation values are interpolated between the two.
            float fractionalBeat = (float)trackInfo.ExactTrackBeat - (int)trackInfo.ExactTrackBeat;

            // if fractionalBeat is less than lastFractionalBeat and intBeat is 0,
            // then we just wrapped around and should increment our completedLoopBeats.
            if (lastFractionalBeat > fractionalBeat)
            {
                completedLoopBeats += 1;
            }
            lastFractionalBeat = fractionalBeat;

            bool beatIsEven = (completedLoopBeats & 0x1) == 0;

            float topDiscTargetYRotationDeg = ((completedLoopBeats / 2) + (beatIsEven ? fractionalBeat : 1)) * 90;
            float bottomDiscTargetYRotationDeg = ((completedLoopBeats / 2) + (beatIsEven ? 0 : fractionalBeat)) * 90;

            for (int i = 0; i < MagicNumbers.OutputBinCount; i++)
            {
                Color discColor = FrequencyBinColor(i);
                if (maxFrequencyAmplitude == 0)
                {
                    Vector3 newLocalScale = new Vector3(
                        originalBandShapeLocalScale.x * MagicNumbers.MinVolumeScale,
                        originalBandShapeLocalScale.y,
                        originalBandShapeLocalScale.z * MagicNumbers.MinVolumeScale);

                    frequencyBandShapes[i].transform.localScale = newLocalScale;

                    discColor *= MagicNumbers.FrequencyShapeMinAlpha;
                }
                else
                {
                    Core.Contract.Assert(!float.IsNaN(signalInfo.Avg));

                    // first, normalize to max
                    float normalizedAmplitude = frequencyBins[i] / maxFrequencyAmplitude;
                    Core.Contract.Assert(!float.IsNaN(normalizedAmplitude));

                    // now take log base 10 (could be 0 if there was only one amplitude value!)
                    float logNormalizedAmplitude = Mathf.Log10(normalizedAmplitude);

                    // now what's the log base 10 of the minimum value? (could also be 0)
                    float logMinimumAmplitude = Mathf.Log10(minFrequencyAmplitude / maxFrequencyAmplitude);
                    float amplitudeRatio, targetValue;
                    if (logMinimumAmplitude == 0)
                    {
                        amplitudeRatio = 0;
                        targetValue = MagicNumbers.MinVolumeScale;
                    }
                    else
                    {
                        // now logNormalizedValue is in the range (logMinimumValue, 0) where logMinimumValue < 0.
                        // let's map this to a ratio from 0 to 1.
                        float positiveLogAmplitude = logNormalizedAmplitude + -logMinimumAmplitude;
                        amplitudeRatio = positiveLogAmplitude / (-logMinimumAmplitude);

                        // now, lerp from a baseline value to ensure zero volume isn't invisible
                        float flooredValue = Mathf.Lerp(MagicNumbers.MinVolumeScale, 1, amplitudeRatio);
                        Core.Contract.Assert(!float.IsNaN(flooredValue));

                        // now, look up what the value was last time
                        float lastFlooredValue = scaledBins[i];
                        Core.Contract.Assert(!float.IsNaN(lastFlooredValue));

                        float decayedValue = lastFlooredValue - ((lastFlooredValue - flooredValue) * MagicNumbers.BinValueDecay);
                        Core.Contract.Assert(!float.IsNaN(decayedValue));

                        targetValue = Mathf.Max(flooredValue, decayedValue);
                        Core.Contract.Assert(!float.IsNaN(targetValue));
                    }

                    Vector3 newLocalScale = new Vector3(
                        originalBandShapeLocalScale.x * targetValue,
                        originalBandShapeLocalScale.y,
                        originalBandShapeLocalScale.z * targetValue);
                    scaledBins[i] = targetValue;

                    frequencyBandShapes[i].transform.localScale = newLocalScale;

                    discColor *= Mathf.Lerp(MagicNumbers.FrequencyShapeMinAlpha, MagicNumbers.FrequencyShapeMaxAlpha, amplitudeRatio);
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

                // and finally, the rotation
                float ratio = (float)i / (MagicNumbers.OutputBinCount - 1);
                float yRotationDeg = bottomDiscTargetYRotationDeg + (ratio * (topDiscTargetYRotationDeg - bottomDiscTargetYRotationDeg));
                frequencyBandShapes[i].transform.localRotation = Quaternion.AngleAxis(yRotationDeg, Vector3.up);
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
            HoloDebug.Log($"LocalLoopie.Initialize: initializing with copied loopie ID {loopie.CopiedLoopieId} and volume {loopie.Volume} at viewpoint position {loopie.ViewpointPosition} with effects {loopie.Effects.ArrayToString()} and levels {loopie.EffectLevels.ArrayToString()}");
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

        #endregion

        #region IEffectable

        public void AlterVolume(float alteration, bool commit)
        {
            float newVolume = loopie.Volume + alteration;
            newVolume = Mathf.Clamp(newVolume, 0, 1);
            //HoloDebug.Log($"LocalLoopie.MultiplyVolume: multiplied by {ratio}, volume is now {loopie.Volume}");

            if (SoundManager.Instance != null)
            {
                NowSoundTrackAPI.SetVolume(trackId, newVolume);
            }

            if (commit)
            {
                loopie.Volume = newVolume;
            }
        }

        public void AlterSoundEffect(EffectId effect, float alteration, bool commit)
        {
            // Is effect present in loopie.Effects already?
            int effectIndex = effect.FindIn(loopie.Effects);
            if (effectIndex == -1)
            {
                // add the effect
                loopie.Effects = effect.AppendTo(loopie.Effects);

                // and set the new level properly
                // TODO: per-effect initial levels
                int initialLevel = 100;
                loopie.EffectLevels = EffectId.AppendTo(loopie.EffectLevels, initialLevel);

                effectIndex = loopie.EffectLevels.Length - 1;

                if (SoundManager.Instance != null)
                {
                    NowSoundTrackAPI.AddPluginInstance(trackId, effect.PluginId.Value, effect.PluginProgramId.Value, initialLevel);
                }
            }

            int newLevel = loopie.EffectLevels[effectIndex] + (int)(alteration * MagicNumbers.EffectLevelScale);
            newLevel = Mathf.Clamp(newLevel, 0, 100);

            //HoloDebug.Log($"LocalLoopie.AlterSoundEffect: id {DistributedObject.Id}, pluginId {effect.PluginId}, programId {effect.PluginProgramId}, alteration {alteration}, newLevel {newLevel}, commit {commit}");

            NowSoundTrackAPI.SetPluginInstanceDryWet(trackId, (PluginInstanceIndex)(effectIndex + 1), newLevel);

            if (commit)
            {
                loopie.EffectLevels[effectIndex] = newLevel;
            }
        }

        public void PopSoundEffect()
        {
            HoloDebug.Log($"LocalLoopie.PopSoundEffect: id {DistributedObject.Id}, {loopie.Effects.Length / 2} effect(s)");

            if (loopie.Effects.Length == 0)
            {
                // nothing to pop
                return;
            }

            loopie.Effects = EffectId.PopFrom(loopie.Effects, 2);
            loopie.EffectLevels = EffectId.PopFrom(loopie.EffectLevels, 1);

            if (SoundManager.Instance != null)
            {
                // +1 here because PluginInstanceIndex is 1-based
                NowSoundTrackAPI.DeletePluginInstance(trackId, (PluginInstanceIndex)(loopie.EffectLevels.Length + 1));
            }
        }

        public void ClearSoundEffects()
        {
            HoloDebug.Log($"LocalLoopie.ClearSoundEffects: id {DistributedObject.Id}, {loopie.Effects.Length} effects");

            if (SoundManager.Instance != null)
            {
                for (int i = 0; i < loopie.Effects.Length; i++)
                {
                    // since the plugin instance indices are just array indices, we can just delete
                    // the first plugin repeatedly until they are all gone
                    NowSoundTrackAPI.DeletePluginInstance(trackId, (PluginInstanceIndex)1);
                }
            }

            loopie.Effects = new int[0];
            loopie.EffectLevels = new int[0];
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
        public bool IsTouched
        {
            get { return isTouched; }
            set
            {
                //HoloDebug.Log($"IsTouched loopie #{this.DistributedObject.Id}: {value}");
                isTouched = value;
            }
        }

        #endregion

        #region Viewpoint-based pan operations

        /// <summary>
        /// Calculate the pan value for a given sound position relative to the sensor.
        /// </summary>
        /// <param name="sensorPosition">The sensor position, in viewpoint (e.g. sensor) coordinates</param>
        /// <param name="sensorForwardDirection">The sensor forward direction, in viewpoint (e.g. sensor) coordinates</param>
        /// <param name="soundPosition">The sound position, in viewpoint (e.g. sensor) coordinates</param>
        /// <returns>A pan value (0 = left, 0.5 = center, 1 = right)</returns>
        public static float CalculatePanValue(Vector3 sensorPosition, Vector3 sensorForwardDirection, Vector3 soundPosition, bool log = false)
        {
            Vector3 flattenedSensorPosition = sensorPosition;
            flattenedSensorPosition.y = 0;
            Vector3 flattenedSoundPosition = soundPosition;
            flattenedSoundPosition.y = 0;

            // find out how close the sensor->head ray is to the sensor forward direction
            Vector3 flattenedSensorForwardDirection = sensorForwardDirection;
            flattenedSensorForwardDirection.y = 0;
            flattenedSensorForwardDirection.Normalize();

            Vector3 flattenedSensorToSoundDirection = (flattenedSoundPosition - flattenedSensorPosition).normalized;

            float soundDirectionDotSensorForwardDirection = Vector3.Dot(flattenedSensorForwardDirection, flattenedSensorToSoundDirection);

            // clamp to between MinDotProductForPanning and 1 (not really possible that user will walk beyond screen edge,
            // but better safe than sorry)
            float soundDirectionDotSensorForwardDirectionClamped = Mathf.Max(soundDirectionDotSensorForwardDirection, MagicNumbers.MinDotProductForPanning);

            // convert to interval between 0 and (1 - MinDotProductForPanning)
            float panValue = soundDirectionDotSensorForwardDirectionClamped - MagicNumbers.MinDotProductForPanning;
            // convert to interval between 0 and 0.5
            panValue *= 0.5f / (1 - MagicNumbers.MinDotProductForPanning);
            panValue = Mathf.Max(0f, Mathf.Min(0.5f, panValue));

            // if sound position is positive X (in viewpoint space), then panning to the right
            if (soundPosition.x >= 0)
            {
                panValue = 1 - panValue;
            }

            if (log)
            {
                // HoloDebug.Log($"LocalLoopie.CalculatePanValue: soundPosition {soundPosition}, soundDirectionDotForwardDirection {soundDirectionDotSensorForwardDirection}, panValue {panValue}");
            }

            // because we are mirrored, invert the pan value
            // TODO: make this a global constant
            bool mirrored = true;
            if (mirrored)
            {
                panValue = 1 - panValue;
            }

            return panValue;
        }

        #endregion
    }
}
