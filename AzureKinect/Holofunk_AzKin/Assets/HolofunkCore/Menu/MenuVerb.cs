using DistributedStateLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Holofunk.Controller.ControllerStateMachine;

namespace Holofunk.Menu
{
    /// <summary>
    /// What kind of menu item is this?
    /// </summary>
    public enum MenuVerbKind
    {
        /// <summary>
        /// The root menu verb; goes at the center, has no label, enables canceling the current verb.
        /// </summary>
        Root = 1,
        /// <summary>
        /// Label only, not directly selectable.
        /// </summary>
        /// <remarks>
        /// If this *is* selected, it is equivalent to selecting the first child.
        /// </remarks>
        Label = 2,
        /// <summary>
        /// Prompt menu action takes place immediately (TODO: and uses square icon).
        /// </summary>
        Prompt = 3,
        /// <summary>
        /// Touch menu action takes place when user touches loopies and hits light/level button.
        /// </summary>
        /// <remarks>
        /// This also supports holding/grabbing.
        /// </remarks>
        Touch = 4,
        /// <summary>
        /// Level menu action creates level widget and allows adjustment by holding light/level button.
        /// </summary>
        Level = 5,
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
       public readonly Func<string> NameFunc;
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
        /// Action executed per-Update on touched loopies and/or performer.
        /// </summary>
        public readonly Action<MenuVerbModel, HashSet<DistributedId>> TouchUpdateAction;
        /// <summary>
        /// Action executed per-Update on all touched loopies and/or performer, based on current level setting, with final commit flag.
        /// </summary>
        public readonly Action<HashSet<DistributedId>, float, bool> LevelUpdateAction;

        /// <summary>
        /// Private ctor, encapsulated by public factory methods
        /// </summary>
        private MenuVerb(
            MenuVerbKind kind,
            Func<string> nameFunc,
            bool mayBePerformer,
            Action promptAction,
            Action<MenuVerbModel, HashSet<DistributedId>> touchUpdateAction,
            Action<HashSet<DistributedId>, float, bool> levelAction)
        {
            Kind = kind;
            NameFunc = nameFunc;
            MayBePerformer = mayBePerformer;
            PromptAction = promptAction;
            TouchUpdateAction = touchUpdateAction;
            LevelUpdateAction = levelAction;
        }

        public static MenuVerb MakeRoot()
        {
            // lol this character doesn't render, oh well, blank is fine
            return new MenuVerb(MenuVerbKind.Root, () => "🚫", false, null, null, null);
        }

        public static MenuVerb MakePrompt(string name, Action action)
        {
            return new MenuVerb(MenuVerbKind.Prompt, () => name, false, action, null, null);
        }

        public static MenuVerb MakePrompt(Func<string> nameFunc, Action action)
        {
            return new MenuVerb(MenuVerbKind.Prompt, nameFunc, false, action, null, null);
        }

        public static MenuVerb MakeTouch(string name, Action<MenuVerbModel, HashSet<DistributedId>> touchUpdateAction)
        {
            return new MenuVerb(MenuVerbKind.Touch, () => name, false, null, touchUpdateAction, null);
        }

        public static MenuVerb MakeLevel(string name, bool mayBePerformer, Action<HashSet<DistributedId>, float, bool> levelUpdateAction)
        {
            return new MenuVerb(MenuVerbKind.Level, () => name, mayBePerformer, null, null, levelUpdateAction);
        }

        public static MenuVerb MakeLabel(string name)
        {
            return new MenuVerb(MenuVerbKind.Label, () => name, false, null, null, null);
        }
    }

}
