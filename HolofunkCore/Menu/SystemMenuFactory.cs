// Copyright by Rob Jellinghaus. All rights reserved.

using System;
using System.Collections.Generic;

namespace Holofunk.Menu
{
    /// <summary>
    /// Creates MenuStructure corresponding to the system menu.
    /// </summary>
    /// <remarks>
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
        public static MenuStructure CreateSystemMenuStructure()
        {
            return new MenuStructure(
                ("BPM", null, new MenuStructure(
                    ("+10", () => { }, null),
                    ("-10", () => { }, null))),
                ("Delete My Sounds", () => { }, null));
        }
    }
}
