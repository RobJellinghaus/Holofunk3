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
    /// 
    /// This should really be a Rust enum :-( So we hack it like one.
    /// </remarks>
    public struct MenuVerb
    {
        /// <summary>
        /// Is this a prompt action or a level-setting action?
        /// </summary>
        public readonly MenuVerbKind Kind;
        /// <summary>
        /// What's the name of this (when stuck to the controller hand)?
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// Action executed promptly on all touched loopies when this prompt verb is selected.
        /// </summary>
        public readonly Action<HashSet<DistributedId>> PromptAction;
        /// <summary>
        /// Action executed per-Update on all touched loopies based on current level setting, with final commit.
        /// </summary>
        public readonly Action<HashSet<DistributedId>, int, bool> LevelAction;

        private MenuVerb(MenuVerbKind kind, string name, Action<HashSet<DistributedId>> promptAction, Action<HashSet<DistributedId>, int, bool> levelAction)
        {
            Kind = kind;
            Name = name;
            PromptAction = promptAction;
            LevelAction = levelAction;
        }

        public static MenuVerb MakePrompt(string name, Action<HashSet<DistributedId>> promptAction)
        {
            return new MenuVerb(MenuVerbKind.Prompt, name, promptAction, null);
        }

        public static MenuVerb MakeLevel(string name, Action<HashSet<DistributedId>, int, bool> levelAction)
        {
            return new MenuVerb(MenuVerbKind.Prompt, name, null, levelAction);
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
