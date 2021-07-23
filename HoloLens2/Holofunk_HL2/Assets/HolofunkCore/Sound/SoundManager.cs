/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Viewpoint;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using NowSoundLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Holofunk.Sound
{
    /// <summary>
    /// Component which manages this app's knowledge of NowSoundLib.
    /// </summary>
    /// <remarks>
    /// Note that this will pull data from a proxy SoundSource object if one exists and
    /// there is no local NowSoundLib. In other words, this object is useful even without
    /// local sound, as it tracks what is known about the remote sound.
    /// </remarks>
    public class SoundManager : MonoBehaviour
    {
        #region Static state

        /// <summary>
        /// The singleton HolofunkController.
        /// </summary>
        private static SoundManager s_instance;

        public static SoundManager Instance { get { return s_instance; } }

        #endregion

        #region Fields

        /// <summary>
        /// The last audio graph state.
        /// </summary>
        private NowSoundGraphState currentAudioGraphState;

        /// <summary>
        /// List of plugins.
        /// </summary>
        private List<string> _plugins = new List<string>();

        /// <summary>
        /// List of programs per plugin.
        /// </summary>
        private readonly List<List<string>> _pluginPrograms = new List<List<string>>();

        /// <summary>
        /// Strings describing plugin instances, per input.
        /// </summary>
        private readonly List<string> _pluginInstanceDescriptionStrings = new List<string>();

        private bool _soundInitialized = false;

        /// <summary>
        /// Are we currently recording output audio into a file?
        /// </summary>
        private bool _isRecordingToFile = false;

        /// <summary>
        /// If true, it's this controller's responsibility to update _theClock.
        /// </summary>
        /// <remarks>
        /// This happens if we are not running under UWP and if we are simulating the audio clock via Unity update loop.
        /// </remarks>
        private bool shouldUpdateClock;

        /// <summary>
        /// If !shouldUpdateClock, then we track the last audio time and advance the clock by the difference.
        /// </summary>
        private Time<AudioSample> lastAudioTime;

        /// <summary>
        /// The fractional samples left over from the last time we advanced the time.
        /// </summary>
        /// <remarks>
        /// This prevents round-off error with advancing the clock. e.g. if we are at 48Khz sample rate and we happen
        /// to always have a remainingTIme of (say) 100.4 samples, then simply rounding down deltaTime would result in
        /// always ignoring the fractional sample, which would soon start drifting from realtime.  Instead, we advance
        /// the clock by 100 samples and save the .4 of a sample in this field; the next time, we will add it to the
        /// deltaTime value giving 100.8 (100 samples output and .8 stored here), and the time after that 101.2, resulting
        /// in 101 samples output and .2 stored back here.
        /// </remarks>
        private float remainingFractionalSample;

        #endregion

        #region Properties

        public bool IsRecordingToFile => _isRecordingToFile;

        public List<string> Plugins => _plugins;
        public List<List<string>> PluginPrograms => _pluginPrograms;

        #endregion

        #region Behaviours

        void Awake()
        {
            // Must only ever be one of these in a scene.
            Contract.Requires(Instance == null);

            s_instance = this;

            // prepopulate the backing strings for this bit of the UI
            _pluginInstanceDescriptionStrings.Add("(none)");
            _pluginInstanceDescriptionStrings.Add("(none)");

            // TODO: move this somewhere else (more generic)
            Application.logMessageReceived += HandleException;
        }

        private void HandleException(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
            {
                string message = $"Unhandled exception: condition {condition}, stack {stackTrace}";
                Debug.LogError(message);
            }
        }

        // Use this for initialization
        void Start()
        {
            ThreadContract.RequireUnity();

            // Verify this large structure is serialized and deserialized correctly.
            TrackInfo trackInfo = NowSoundTrackAPI.GetStaticTrackInfo();

            Contract.Assert(trackInfo.IsTrackLooping);
            Contract.Assert(trackInfo.StartTime == 2);
            Contract.Assert((float)trackInfo.StartTimeInBeats == 3);
            Contract.Assert(trackInfo.Duration == 4);
            Contract.Assert(trackInfo.DurationInBeats == 5);
            Contract.Assert((float)trackInfo.ExactDuration == 6);
            Contract.Assert(trackInfo.LocalClockTime == 7);
            Contract.Assert((float)trackInfo.LocalClockBeat == 8);
            Contract.Assert(trackInfo.LastSampleTime == 9);
            Contract.Assert(trackInfo.Pan == 10);

            // If InitializeClock has not been called, then we need to update the Clock ourselves.
            if (Clock.PossiblyNullInstance == null)
            {
                new Clock(beatsPerMinute: 90f, beatsPerMeasure: 4, inputChannelCount: 2); // initializes Clock.Instance

                // audio clock is good
                shouldUpdateClock = false;
            }

            // InstantiatePlayerControllers();

            StartCoroutine(InitializeAudioGraph());
        }

        const int logMessageCapacity = 256;

        public void WriteAllLogMessagesToUnityDebugConsole()
        {
            Debug.developerConsoleVisible = true;

            NowSoundLogInfo logInfo = NowSoundGraphAPI.LogInfo();
            StringBuilder builder = new StringBuilder(logMessageCapacity);
            if (logInfo.LogMessageCount > 0)
            {
                for (int i = 0; i < logInfo.LogMessageCount; i++)
                {
                    builder.Clear();
                    NowSoundGraphAPI.GetLogMessage(i, builder);
                    Debug.Log($"NOWSOUNDLIB LOG: {builder}");
                }
                NowSoundGraphAPI.DropLogMessages(logInfo.LogMessageCount);
            }
        }

        // Audio graph initialization is a multi-step asynchronous process.
        // Fortunately, the co_await style used in NowSoundLib is (unsurprisingly) a good match for Unity coroutines.
        // We implement all async support in Unity using coroutines that poll the graph.  This can introduce a
        // multi-frame delay in asynchronous operations that have to poll for graph state change, but this is considered
        // a lot easier than implementing callouts from NowSoundLib via P/Invoke up to Unity.
        public IEnumerator InitializeAudioGraph()
        {
            // TODO: all of this... but let's just get this compiling first
            yield return WaitForGraphState(NowSoundGraphState.GraphUninitialized);

            NowSoundGraphAPI.InitializeInstance(
                MagicNumbers.OutputBinCount,
                MagicNumbers.CentralFrequency,
                MagicNumbers.OctaveDivisions,
                MagicNumbers.CentralFrequencyBin,
                MagicNumbers.FftBinSize);

            // now there should be some log messages
            WriteAllLogMessagesToUnityDebugConsole();

            yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

            // HACK: hardcode device 0
            // TODO: use GUI for device selection again
            // NowSoundGraphAPI.InitializeDeviceInputs(0);

            NowSoundGraphInfo graphInfo = NowSoundGraphAPI.Info();

            // Contract.Assert(graphInfo.InputDeviceCount == 2);

            // initialize the last audio time
            TimeInfo time = NowSoundGraphAPI.TimeInfo();
            lastAudioTime = time.TimeInSamples;

            // Initialize plugins!
            // Now let's scan!
            NowSoundGraphAPI.AddPluginSearchPath(@"C:\Program Files\Steinberg\VSTPlugins");
            NowSoundGraphAPI.AddPluginSearchPath(@"C:\Program Files\VSTPlugins");

            bool searched = NowSoundGraphAPI.SearchPluginsSynchronously();
            Contract.Assert(searched);

            // try to let some time pass just in case
            yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

            int pluginCount = NowSoundGraphAPI.PluginCount();
            StringBuilder buffer = new StringBuilder(100);

            for (int pluginIndex = 1; pluginIndex <= pluginCount; pluginIndex++)
            {
                NowSoundGraphAPI.PluginName((PluginId)pluginIndex, buffer);
                string pluginName = buffer.ToString();
                Debug.Log($"Plugin #{pluginIndex}: '{pluginName}'");
                _plugins.Add(pluginName);

                List<string> programs = new List<string>();
                yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

                Debug.Log($"    Calling LoadPluginPrograms...");

                string presetPath = $@"C:\git\holofunk2\presets\{pluginName}";
                if (Directory.Exists(presetPath))
                {
                    NowSoundGraphAPI.LoadPluginPrograms((PluginId)pluginIndex, presetPath);

                    yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

                    int programCount = NowSoundGraphAPI.PluginProgramCount((PluginId)pluginIndex);
                    Debug.Log($"    {programCount} programs loaded.");
                    for (int programIndex = 1; programIndex <= programCount; programIndex++)
                    {
                        NowSoundGraphAPI.PluginProgramName((PluginId)pluginIndex, (ProgramId)programIndex, buffer);
                        string programName = buffer.ToString();
                        programs.Add(programName);
                        Debug.Log($"    Program #{programIndex}: '{programName}'");
                    }
                }

                _pluginPrograms.Add(programs);
            }

            _soundInitialized = true;
        }

        IEnumerator WaitForGraphStateCoroutine(NowSoundGraphState expectedState)
        {
            while (true)
            {
                NowSoundGraphState currentState = NowSoundGraphAPI.State();
                if (currentState == expectedState)
                {
                    // done!
                    currentAudioGraphState = currentState;
                    yield break;
                }
                else
                {
                    Debug.Log($"HolofunkController.WaitForGraphStateCoroutine: currentState {currentState} != expectedState {expectedState}");
                }
                // wait for a frame
                yield return null;
            }
        }

        // Wait for the graph state, expressed as a waitable coroutine.
        Coroutine WaitForGraphState(NowSoundGraphState expectedState)
        {
            return StartCoroutine(WaitForGraphStateCoroutine(expectedState));
        }

        public void UpdateInputEffectString(AudioInputId audioInputId)
        {
            string s;
            {
                int effectCount = NowSoundGraphAPI.GetInputPluginInstanceCount(audioInputId);
                if (effectCount == 0)
                {
                    s = "(none)";
                }
                else
                {
                    List<string> programs = new List<string>();
                    for (int i = 0; i < effectCount; i++)
                    {
                        PluginInstanceInfo pluginInstanceInfo = NowSoundGraphAPI.GetInputPluginInstanceInfo(audioInputId, (PluginInstanceIndex)(i + 1));
                        programs.Add(_pluginPrograms[(int)pluginInstanceInfo.NowSoundPluginId - 1][(int)pluginInstanceInfo.NowSoundProgramId - 1]);
                    }
                    s = string.Join(", ", programs);
                }
            }
            _pluginInstanceDescriptionStrings[(int)audioInputId - 1] = s;
        }

        void Update()
        {
            TimeInfo timeInfo = NowSoundGraphAPI.TimeInfo();
            if (_soundInitialized)
            {
                /*
                // for now co-opt TheText to display time info
                int pluginCount = NowSoundGraphAPI.PluginCount();
                GUIController.Instance.Text1 =
                    $@"{timeInfo.BeatsPerMinute} BPM - {(int)timeInfo.ExactBeat} total beats - measure beat #{(timeInfo.BeatInMeasure):0.##}
#1 FX: {_pluginInstanceDescriptionStrings[0]} - #2 FX: {_pluginInstanceDescriptionStrings[1]}";
                */
            }

            // Call OnPreUpdate on all Loopies.
            // We can't use ExecuteEventOnChildren for this because the loopies aren't actually children of the LoopieContainer,
            // since this puts them in the wrong transform for easy world space manipulation.
            // So we hack it with the LoopieController's static Loopie collection.
            /*
            LoopieController.PreUpdateAllLoopies();
            // However, the diegetic canvas lets us PreUpdate just fine.
            preUpdateEventExecutor.ExecuteEventOnChildren(
                GameObject.Find(HolofunkGUIStateMachine.UIContainer),
                false,
                (child, state) => child.OnPreUpdate());
            */

            // If there is no external clock, then use deltaTime to update this clock, taking care not to drop fractional samples.
            if (shouldUpdateClock)
            {
                float samples = Time.deltaTime * Clock.SampleRateHz;
                int truncatedSamples = (int)(samples + float.Epsilon); // add Float.Epsilon in case of 1.9999999-type cases
                float fractionalSample = samples - truncatedSamples;
                Contract.Assert(fractionalSample >= 0);

                int sampleCount = truncatedSamples;

                remainingFractionalSample += fractionalSample;
                if (remainingFractionalSample >= 1f)
                {
                    int extraSamples = (int)remainingFractionalSample;
                    sampleCount += extraSamples;
                    remainingFractionalSample = remainingFractionalSample - extraSamples;
                    Contract.Assert(remainingFractionalSample >= 0f);
                    Contract.Assert(remainingFractionalSample < 1f);
                }

                Clock.Instance.AdvanceFromUnity(sampleCount);
            }
            else if (currentAudioGraphState == NowSoundGraphState.GraphRunning)
            {
                // lastAudioTime has been initialized, so the graph must be running, so it's safe to call GetTimeInfo()

                Clock.Instance.UpdateFromAudioGraph(timeInfo.TimeInSamples);

                // TODO: and update the text

                // ... and call the bogus hacked MessageTick() method
                NowSoundGraphAPI.MessageTick();
            }

            // Update the value of UnityNow; this will be the value for all other Update()s in this cycle.
            Clock.Instance.SynchronizeUnityTimeWithAudioTime();

            /*
            // TODO: remove this
            // Temporary: put in an actual wall-clock clock
            DateTime dateTimeNow = DateTime.Now;
            // TODO: this could be horribly garbagey, let's see
            KinectManager.Instance.calibrationText.text = string.Format("{0}:{1}:{2}.{3}", dateTimeNow.Hour, dateTimeNow.Minute, dateTimeNow.Second, dateTimeNow.Millisecond);
            */

            // pick up any log messages
            WriteAllLogMessagesToUnityDebugConsole();
        }

        #endregion
    }
}
