// Copyright by Rob Jellinghaus. All rights reserved.

using System;
using System.Collections.Generic;

namespace Holofunk.Menu
{
    /// <summary>
    /// Describes the visual structure of one level of a menu.
    /// </summary>
    public class MenuStructure
    {
        private List<(string, Action, MenuStructure)> items = new List<(string, Action, MenuStructure)>();

        public MenuStructure(params (string, Action, MenuStructure)[] args)
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
        public string Name(MenuItemId id) => items[id.AsIndex].Item1;

        public Action Action(MenuItemId id) => items[id.AsIndex].Item2;

        public MenuStructure Child(MenuItemId id) => items[id.AsIndex].Item3;
    }
}
