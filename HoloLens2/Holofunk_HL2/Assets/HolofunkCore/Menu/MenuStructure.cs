// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using System;
using System.Collections.Generic;

namespace Holofunk.Menu
{
    /// <summary>
    /// Describes the visual structure of one level of a menu.
    /// </summary>
    public class MenuStructure
    {
        private List<(string, Action<HashSet<DistributedId>>, MenuStructure)> items = 
            new List<(string, Action<HashSet<DistributedId>>, MenuStructure)>();

        public MenuStructure(params (string, Action<HashSet<DistributedId>>, MenuStructure)[] args)
        {
            foreach (var arg in args)
            {
                Core.Contract.Assert(arg.Item1 != null);
                Core.Contract.Assert(arg.Item2 == null || arg.Item3 == null);
            }
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

        /// <summary>
        /// Get the action associated with this item, which takes a list of the currently touched loopies.
        /// </summary>
        /// <remarks>
        /// If the item has children (e.g. a submenu), it should not have its own action.
        /// </remarks>
        public Action<HashSet<DistributedId>> Action(MenuItemId id) => items[id.AsIndex].Item2;

        /// <summary>
        /// Get the child MenuStructure (e.g. submenu structure), if any.
        /// </summary>
        public MenuStructure Child(MenuItemId id) => items[id.AsIndex].Item3;
    }
}
