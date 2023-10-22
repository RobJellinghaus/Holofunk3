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
        /// Touch menu action takes place when user touches loopies and hits light button.
        /// </summary>
        Touch = 2,
        /// <summary>
        /// Level menu action creates level widget and allows adjustment.
        /// </summary>
        Level = 3,
        /// <summary>
        /// Label only, not directly selectable.
        /// </summary>
        Label = 4,
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
        /// If this is a level menu verb, can this affect the performer?
        /// </summary>
        /// <remarks>
        /// This is false for volume level setting, and true for all other effects.
        /// </remarks>
        public readonly bool MayBePerformer;
        /// <summary>
        /// Action executed promptly when this prompt verb is selected.
        /// </summary>
        public readonly Action PromptAction;
        /// <summary>
        /// Action executed on touched loopies as soon as they are touched with the Light button.
        /// </summary>
        public readonly Action<HashSet<DistributedId>> TouchAction;
        /// <summary>
        /// Action executed per-Update on all touched loopies and/or performer based on current level setting, with final commit.
        /// </summary>
        public readonly Action<HashSet<DistributedId>, float, bool> LevelAction;

        public static MenuVerb Undefined => new MenuVerb(MenuVerbKind.Undefined, null, false, null, null, null);

        public bool IsDefined => Kind != MenuVerbKind.Undefined;

        private MenuVerb(
            MenuVerbKind kind,
            string name,
            bool mayBePerformer,
            Action promptAction,
            Action<HashSet<DistributedId>> touchAction,
            Action<HashSet<DistributedId>, float, bool> levelAction)
        {
            Kind = kind;
            Name = name;
            MayBePerformer = mayBePerformer;
            PromptAction = promptAction;
            TouchAction = touchAction;
            LevelAction = levelAction;
        }

        public static MenuVerb MakePrompt(string name, Action action)
        {
            return new MenuVerb(MenuVerbKind.Prompt, name, false, action, null, null);
        }

        public static MenuVerb MakeTouch(string name, Action<HashSet<DistributedId>> touchAction)
        {
            return new MenuVerb(MenuVerbKind.Touch, name, false, null, touchAction, null);
        }

        public static MenuVerb MakeLevel(string name, bool mayBePerformer, Action<HashSet<DistributedId>, float, bool> levelAction)
        {
            return new MenuVerb(MenuVerbKind.Level, name, mayBePerformer, null, null, levelAction);
        }

        public static MenuVerb MakeLabel(string name)
        {
            return new MenuVerb(MenuVerbKind.Label, name, false, null, null, null);
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
