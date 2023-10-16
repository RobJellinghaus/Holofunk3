// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using System;
using System.Collections.Generic;

namespace Holofunk.Menu
{
    public enum MenuVerbKind
    {
        Undefined = 0,
        /// <summary>
        /// Prompt menu action takes place immediately and uses square icon.
        /// </summary>
        Prompt = 1,
        /// <summary>
        /// Level menu action creates level widget and allows adjustment.
        /// </summary>
        Level = 2,
    }

    /// <summary>
    /// High-level description of a menu's behavior.
    /// </summary>
    /// <remarks>
    /// Menu items update their PPlusController's selected MenuVerb when a menu item is picked.
    /// </remarks>
    public struct MenuVerb
    {
        public readonly MenuVerbKind Kind;
        public readonly string Name;
        public readonly Action<HashSet<DistributedId>> Action;

        public MenuVerb(MenuVerbKind kind, string name, Action<HashSet<DistributedId>> action)
        {
            Kind = kind;
            Name = name;
            Action = action;
        }
    }

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
