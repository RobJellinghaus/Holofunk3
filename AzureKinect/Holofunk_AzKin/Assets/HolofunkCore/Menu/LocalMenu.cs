﻿// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Sound;
using Holofunk.Viewpoint;
using NowSoundLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Holofunk.Menu
{
    /// <summary>
    /// The local implementation of a Menu object.
    /// </summary>
    public class LocalMenu : MonoBehaviour, IDistributedMenu, ILocalObject
    {
        #region Fields

        /// <summary>
        /// The state of this Menu, in distributed terms.
        /// </summary>
        private MenuState menuState;

        /// <summary>
        /// The menu state as of the end of the last Update() cycle.
        /// </summary>
        private MenuState priorMenuState;

        /// <summary>
        /// The list of currently open menu levels.
        /// </summary>
        List<MenuLevel> menuLevels = new List<MenuLevel>();

        /// <summary>
        /// The full menu structure for this menu (e.g. the top level, containing all sublevels).
        /// </summary>
        MenuStructure menuStructure;

        #endregion

        #region IDistributedMenu

        public void Initialize(MenuState menuState)
        {
            this.menuState = menuState;

            if (menuState.MenuKind.Value == MenuKinds.System)
            {
                // TODO: parameterize this properly
                menuStructure = SystemMenuFactory.CreateSystemMenuStructure();
            }

            // create the root menu level
            menuLevels.Add(new MenuLevel(this, 0, menuStructure, Vector3.zero));
        }

        public MenuState MenuState => menuState;

        public IDistributedObject DistributedObject => throw new NotImplementedException();

        public void SetSelection(MenuItemId topSelectedItem, MenuItemId subSelectedItem)
        {
            // If toplevel item is not initialized, then sub item must also be not initialized.
            Core.Contract.Assert(topSelectedItem.IsInitialized ? true : !subSelectedItem.IsInitialized);

            MenuState currentState = MenuState;
            currentState.TopSelectedItem = topSelectedItem;
            currentState.SubSelectedItem = subSelectedItem;
            menuState = currentState;
        }

        public void InvokeSelectedAction()
        {
            MenuState state = MenuState;
            if (!state.TopSelectedItem.IsInitialized)
            {
                // was nothing to do
                return;
            }

            // ok top item is known initialized. get its structure entry
            Action action = menuStructure.Action(state.TopSelectedItem);

            if (state.SubSelectedItem.IsInitialized)
            {
                MenuStructure childStructure = menuStructure.Child(state.TopSelectedItem);
                action = childStructure.Action(state.SubSelectedItem);
            }

            action();
        }

        public void OnDelete()
        {
            // nothing to do... gameobject deletion is sufficient
        }

        #endregion

        #region MonoBehaviour

        public void Start()
        {
        }

        private void InstantiateMenuObjects()
        {
        }

        /// <summary>
        /// Update the current menu and possibly submenu(s) to match current selection state.
        /// </summary>
        /// <remarks>
        /// This method may update the menuLevels list.
        /// </remarks>
        public void Update()
        {
            // Our selection state may have changed.
            // If so, we want to update our list of MenuLevels appropriately for the new state.
            if (priorMenuState.TopSelectedItem != MenuState.TopSelectedItem)
            {
                // if we have a sub-level, close it
                if (menuLevels.Count > 1)
                {
                    menuLevels[1].Destroy();
                    menuLevels.RemoveAt(1);
                }

                if (priorMenuState.TopSelectedItem.IsInitialized)
                {
                    menuLevels[0].ColorizeMenuItem(priorMenuState.TopSelectedItem.Value - 1, Color.grey);
                }

                if (MenuState.TopSelectedItem.IsInitialized)
                {
                    // is there a submenu? if so, create it
                    if (MenuState.SubSelectedItem.IsInitialized)
                    {
                        MenuLevel newSubMenu = new MenuLevel(
                            this,
                            1,
                            menuStructure.Child(menuState.SubSelectedItem.Value - 1),
                            Vector3.zero);
                        newSubMenu.ColorizeMenuItem(MenuState.SubSelectedItem.Value - 1, Color.white);
                    }
                    else
                    {
                        menuLevels[0].ColorizeMenuItem(MenuState.TopSelectedItem.Value - 1, Color.white);
                    }
                }
            }
            else if (priorMenuState.SubSelectedItem != MenuState.SubSelectedItem)
            {
                if (priorMenuState.SubSelectedItem.IsInitialized)
                {
                    menuLevels[1].ColorizeMenuItem(priorMenuState.SubSelectedItem.Value - 1, Color.grey);
                }

                if (MenuState.SubSelectedItem.IsInitialized)
                {
                    menuLevels[1].ColorizeMenuItem(MenuState.SubSelectedItem.Value - 1, Color.white);
                }
            }

            priorMenuState = MenuState;
        }

        #endregion

        #region Menu levels

        /// <summary>
        /// Get the closest menu item to this hand position.
        /// </summary>
        /// <returns>A tuple of (depth, menu-item-id), or None if no menu item is close enough.</returns>
        public Option<(int, MenuItemId)> GetClosestMenuItemIfAny(Vector3 handPosition)
        {
            // Now the goal is to find, for all active menu controllers, which menu item is closest to the hand?
            // Once we know that, we will determine whether that is a change of the currently selected leaf menu item,
            // or whether it is a change in selection for some parent menu item.  If it is the latter, we will close
            // and open menus as appropriate.
            // The "distance" is actually measured as the angular proximity (dot product) of two camera rays towards
            // the menu item location and the hand location.

            // First, find the closest item.
            int closestMenuIndex = -1;
            float largestDotProduct = 0;
            int closestMenuItemIndex = -1;

            for (int i = 0; i < menuLevels.Count; i++)
            {
                MenuLevel menuLevel = menuLevels[i];
                //_logBuffer.Append($"Checking menu #{i}...{Environment.NewLine}");
                Option<Tuple<float, int>> result = menuLevel.GetClosestMenuItemIfAny(handPosition);

                if (result.HasValue)
                {
                    if (result.Value.Item1 > largestDotProduct)
                    {
                        largestDotProduct = result.Value.Item1;
                        closestMenuIndex = i;
                        closestMenuItemIndex = result.Value.Item2;

                        //_logBuffer.Append($"New closest item: menu #{i}, dot product {largestDotProduct}, item #{closestMenuItemIndex}{Environment.NewLine}");
                    }
                }
            }

            if (closestMenuIndex == -1)
            {
                return Option<(int, MenuItemId)>.None;
            }
            else
            {
                return (closestMenuIndex, closestMenuItemIndex);
            }
        }

        #endregion
    }
}
