// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Sound;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Menu
{
    /// <summary>
    /// The distributed interface of a Menu.
    /// </summary>
    /// <remarks>
    /// This supports exactly two-level hierarchical menus.
    /// 
    /// Currently menus are created with a fixed location and orientation that does not change thereafter,
    /// so only the menu's selection state is mutable after creation.
    /// 
    /// Note that this is only for (distributed) display of the menu; the menu's actual interaction code
    /// is not part of the distributed menu object, but rather exists and runs only on the host that owns
    /// the menu.
    /// </remarks>
    public interface IDistributedMenu : IDistributedInterface
    {
        /// <summary>
        /// The selection state of the Menu.
        /// </summary>
        [LocalMethod]
        MenuState MenuState { get; }

        /// <summary>
        /// Change the menu's selection.
        /// </summary>
        [ReliableMethod]
        void SetSelection(MenuItemId topSelectedItemId, MenuItemId subSelectedItemId);

        /// <summary>
        /// Invoke the action associated with the currently selected (sub)menu item.
        /// </summary>
        /// <remarks>
        /// This will only be invoked on the host which owns the menu, which is good as that is the only
        /// host which defines any actions in the menu structure.
        /// 
        /// Note that this will have MenuVerbKind.Undefined if there is no selected menu item (or if the
        /// user unselects the current verb).
        /// </remarks>
        [LocalMethod]
        MenuVerb GetMenuVerb();
    }
}
