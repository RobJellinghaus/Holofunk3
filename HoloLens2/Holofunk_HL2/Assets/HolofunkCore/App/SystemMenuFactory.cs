// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Sound;
using System;
using Holofunk.Menu;
using Holofunk.Viewpoint;
using System.Collections.Generic;
using Distributed.State;

namespace Holofunk.App
{
    /// <summary>
    /// Creates MenuStructure corresponding to the system menu.
    /// </summary>
    /// <remarks>
    /// This is in the Holofunk.App namespace because it refers to many different packages to implement its,
    /// functionality, and we want the Holofunk.Menu namespace to not include all these other dependencies.
    /// 
    /// The actions populating this structure will be null if the arguments to CreateSystemMenuStructure are null.
    /// This is the intended situation when creating a menu proxy, which only needs the items, but not the actions.
    /// </remarks>
    public static class SystemMenuFactory
    {
        /// <summary>
        /// Construct a MenuStructure with the given context.
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public static MenuStructure Create()
        {
            DistributedSoundClock clock1 = DistributedSoundClock.Instance;
            float bpm = clock1.TimeInfo.Value.BeatsPerMinute;

            bool isViewpoint = false;
            bool isRecording = false;
            if (DistributedViewpoint.Instance != null)            
            {
                isViewpoint = true;
                isRecording = DistributedViewpoint.Instance.IsRecording;
            }

            Action<int> setBPMAction = delta =>
            {
                DistributedSoundClock clock2 = DistributedSoundClock.Instance;
                if (clock2 != null)
                {
                    float newBPM = clock2.TimeInfo.Value.BeatsPerMinute + delta;
                    if (newBPM > 0)
                    {
                        clock2.SetBeatsPerMinute(newBPM);
                    }
                }
            };

            Action<bool> setRecordingAction = beRecording =>
            {
                if (beRecording)
                {
                    DistributedViewpoint.Instance.StartRecording();
                }
                else
                {
                    DistributedViewpoint.Instance.StopRecording();
                }
            };

            List<(string, Action<HashSet<DistributedId>>, MenuStructure)> items = 
                new List<(string, Action<HashSet<DistributedId>>, MenuStructure)>();

            items.Add(("BPM", null, new MenuStructure(
                    ($"=>{bpm + 10}", _ => setBPMAction(10), null),
                    ($"{bpm - 10}<=", _ => setBPMAction(-10), null))));

            items.Add(("Delete My Sounds", _ => { }, null));

            if (isViewpoint)
            {
                if (isRecording)
                {
                    items.Add(("Stop\nRecording", _ => setRecordingAction(false), null));
                }
                else
                {
                    items.Add(("Start\nRecording", _ => setRecordingAction(true), null));
                }
            }

            return new MenuStructure(items.ToArray());
        }
    }
}
