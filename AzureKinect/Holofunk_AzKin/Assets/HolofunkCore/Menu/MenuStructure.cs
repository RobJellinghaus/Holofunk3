// Copyright by Rob Jellinghaus. All rights reserved.

using DistributedStateLib;
using Holofunk.Controller;
using System;
using System.Collections.Generic;
using static Holofunk.Controller.ControllerStateMachine;

namespace Holofunk.Menu
{
    /// <summary>
    /// Describes the visual structure of one level of a menu.
    /// </summary>
    public class MenuStructure
    {
        /// <summary>
        /// The items in this level of the menu. Each MenuVerb may have a sub-menu which is the second item in the tuple.
        /// </summary>
        private List<(MenuVerb, MenuStructure)> items = 
            new List<(MenuVerb, MenuStructure)>();

        public MenuStructure(params (MenuVerb, MenuStructure)[] args)
        {
            items.AddRange(args);
        }

        /// <summary>
        /// Count of menu items at this level.
        /// </summary>
        public int Count => items.Count;

        /// <summary>
        /// Get the menu item name at this index.
        /// </summary>
        public MenuVerb Verb(MenuItemId id) => items[id.AsIndex].Item1;

        /// <summary>
        /// Get the child MenuStructure (e.g. submenu structure), if any.
        /// </summary>
        public MenuStructure Child(MenuItemId id) => items[id.AsIndex].Item2;
    }
}
