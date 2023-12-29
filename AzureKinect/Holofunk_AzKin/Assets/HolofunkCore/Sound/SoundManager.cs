/// Copyright by Rob Jellinghaus.  All rights reserved.

using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Hand;
using Holofunk.Viewpoint;
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
    /// This component exists only where sound is being hosted.
    /// </remarks>
    public class SoundManager : MonoBehaviour
    {
        #region Static state

        /// <summary>
        /// The singleton SoundManager; if this is null, there is no local sound support.
        /// </summary>
        public static SoundManager Instance { get; private set; }

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

        /// <summary>
        /// Are we currently recording output audio into a file?
        /// </summary>
        private bool _isRecordingToFile = false;

        #endregion

        #region Properties

        public bool IsRunning => currentAudioGraphState == NowSoundGraphState.GraphRunning;
        public bool IsError => currentAudioGraphState == NowSoundGraphState.GraphInError;

        public bool IsRecordingToFile => _isRecordingToFile;

        public List<string> Plugins => _plugins;
        public List<List<string>> PluginPrograms => _pluginPrograms;

        #endregion

        #region Behaviours

        void Awake()
        {
            // Must only ever be one of these in a scene.
            Contract.Requires(Instance == null);

            Instance = this;

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

            Contract.Assert(trackInfo.Value.IsTrackLooping);
            Contract.Assert(trackInfo.Value.StartTime == 2);
            Contract.Assert((float)trackInfo.Value.StartTimeInBeats == 3);
            Contract.Assert(trackInfo.Value.Duration == 4);
            Contract.Assert(trackInfo.Value.DurationInBeats == 5);
            Contract.Assert((float)trackInfo.Value.ExactDuration == 6);
            Contract.Assert(trackInfo.Value.LocalClockTime == 7);
            Contract.Assert((float)trackInfo.Value.LocalClockBeat == 8);
            Contract.Assert(trackInfo.Value.LastSampleTime == 9);
            Contract.Assert(trackInfo.Value.Pan == 10);

            // Create the distributed sound clock.
            // Note that this initializes the DistributedSoundClock.Instance field.
            DistributedSoundClock.Create(new TimeInfo(new NowSoundLib.TimeInfo(4, 90f, 0, 0)));

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
                MagicNumbers.FftBinSize,
                (float)MagicNumbers.PreRecordingDuration);

            // now there should be some log messages
            WriteAllLogMessagesToUnityDebugConsole();

            yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

            // HACK: hardcode device 0
            // TODO: use GUI for device selection again
            // NowSoundGraphAPI.InitializeDeviceInputs(0);

            // NowSoundGraphInfo graphInfo = NowSoundGraphAPI.Info();
            // Contract.Assert(graphInfo.InputDeviceCount == 2);

            // Initialize plugins!
            // Now let's scan!
            // TODO: make this something that could actually ship (environment variable???)
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
                NowSoundGraphAPI.PluginName((NowSoundLib.PluginId)pluginIndex, buffer);
                string pluginName = buffer.ToString();
                Debug.Log($"Plugin #{pluginIndex}: '{pluginName}'");
                _plugins.Add(pluginName);

                List<string> programs = new List<string>();
                yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

                Debug.Log($"    Calling LoadPluginPrograms...");

                // TODO: gaaaah make this something that could ship
                string presetPath = $@"C:\git\holofunk3\azurekinect\presets\{pluginName}";
                if (Directory.Exists(presetPath))
                {
                    NowSoundGraphAPI.LoadPluginPrograms((NowSoundLib.PluginId)pluginIndex, presetPath);

                    yield return WaitForGraphState(NowSoundGraphState.GraphRunning);

                    int programCount = NowSoundGraphAPI.PluginProgramCount((NowSoundLib.PluginId)pluginIndex);
                    Debug.Log($"    {programCount} programs loaded.");
                    for (int programIndex = 1; programIndex <= programCount; programIndex++)
                    {
                        NowSoundGraphAPI.PluginProgramName((NowSoundLib.PluginId)pluginIndex, (ProgramId)programIndex, buffer);
                        string programName = buffer.ToString();
                        programs.Add(programName);
                        Debug.Log($"    Program #{programIndex}: '{programName}'");
                    }
                }

                _pluginPrograms.Add(programs);
            }

            // and create all the sound effect objects!
            for (int i = 0; i < _plugins.Count; i++)
            {
                int pluginId = i + 1;
                string pluginName = _plugins[i];

                for (int j = 0; j < _pluginPrograms[i].Count; j++)
                {
                    int pluginProgramId = j + 1;
                    string programName = _pluginPrograms[i][j];

                    // Create distributed sound effect instance to disseminate knowledge of this effect.
                    // We only need to create it; it will get propagated and added to the scene graph, so we
                    // don't have to retain the return value.
                    DistributedSoundEffect.Create(
                        new PluginId((NowSoundLib.PluginId)pluginId),
                        pluginName,
                        new PluginProgramId((ProgramId)pluginProgramId),
                        programName);
                }
            }
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
                int effectCount = NowSoundGraphAPI.GetInputPluginInstanceCount(audioInputId.Value);
                if (effectCount == 0)
                {
                    s = "(none)";
                }
                else
                {
                    List<string> programs = new List<string>();
                    for (int i = 0; i < effectCount; i++)
                    {
                        PluginInstanceInfo pluginInstanceInfo = NowSoundGraphAPI.GetInputPluginInstanceInfo(audioInputId.Value, (PluginInstanceIndex)(i + 1));
                        programs.Add(_pluginPrograms[(int)pluginInstanceInfo.NowSoundPluginId - 1][(int)pluginInstanceInfo.NowSoundProgramId - 1]);
                    }
                    s = string.Join(", ", programs);
                }
            }
            _pluginInstanceDescriptionStrings[(int)audioInputId.Value - 1] = s;
        }

        void Update()
        {
            TimeInfo timeInfo = NowSoundGraphAPI.TimeInfo();

            if (currentAudioGraphState == NowSoundGraphState.GraphRunning)
            {
                // update the distributed clock instance, which must exist
                if (DistributedSoundClock.Instance != null)
                {
                    DistributedSoundClock.Instance.UpdateTimeInfo(timeInfo);
                }

                // ... and call the bogus hacked MessageTick() method
                NowSoundGraphAPI.MessageTick();
            }

            // pick up any log messages
            WriteAllLogMessagesToUnityDebugConsole();
        }

        public void OnApplicationQuit()
        {
            // shhhhh
            NowSoundGraphAPI.ShutdownInstance();
        }

        #endregion

        #region Recording control

        public void StartRecording()
        {
            if (!IsRecordingToFile)
            {
                string myDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string recordingsPath = Path.Combine(myDocumentsPath, "HolofunkRecordings");
                Directory.CreateDirectory(recordingsPath);

                DateTime now = DateTime.Now;
                string recordingFile = Path.Combine(recordingsPath, now.ToString("yyyyMMdd_HHmmss.wav"));

                _isRecordingToFile = true;
                NowSoundGraphAPI.StartRecording(recordingFile);

                HoloDebug.Log($"SoundManger.StartRecording: started recording to {recordingFile}");
            }
        }

        public void StopRecording()
        {
            if (IsRecordingToFile)
            {
                _isRecordingToFile = false;
                NowSoundGraphAPI.StopRecording();

                HoloDebug.Log($"SoundManger.StopRecording: stopped recording");
            }
        }

        #endregion

        #region State update

        public void SetBeatsPerMinute(float newBPM)
        {
            NowSoundGraphAPI.SetBeatsPerMinute(newBPM);
        }

        #endregion

    }
}
