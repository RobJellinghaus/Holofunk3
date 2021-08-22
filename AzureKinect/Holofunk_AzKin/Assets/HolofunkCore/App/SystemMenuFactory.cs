// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Sound;
using System;
using Holofunk.Menu;

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
            Action<int> setBPMAction = delta =>
            {
                DistributedSoundClock theClock = DistributedSoundClock.Instance;
                if (theClock != null)
                {
                    float newBPM = theClock.TimeInfo.Value.BeatsPerMinute + delta;
                    if (newBPM > 0)
                    {
                        theClock.SetBeatsPerMinute(newBPM);
                    }
                }
            };

            return new MenuStructure(
                ("BPM", null, new MenuStructure(
                    ("+10", () => setBPMAction(10), null),
                    ("-10", () => setBPMAction(-10), null))),
                ("Delete My Sounds", () => { }, null));
        }
    }
}
