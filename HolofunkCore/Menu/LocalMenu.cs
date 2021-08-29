// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Shape;
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
                // TODO: figure out how to structure the package dependencies here; Menu shouldn't depend on App
                menuStructure = App.SystemMenuFactory.Create();
            }
            else if (menuState.MenuKind.Value == MenuKinds.SoundEffects)
            {
                // TODO: parameterize this properly
                // TODO: figure out how to structure the package dependencies here; Menu shouldn't depend on App
                menuStructure = App.SoundEffectsMenuFactory.Create();
            }

            // create the root menu level
            menuLevels.Add(new MenuLevel(this, Vector3.zero, 0, menuStructure));
        }

        public MenuState MenuState => menuState;

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedMenu>();

        public void SetSelection(MenuItemId topSelectedItem, MenuItemId subSelectedItem)
        {
            // If toplevel item is not initialized, then sub item must also be not initialized.
            Core.Contract.Assert(topSelectedItem.IsInitialized ? true : !subSelectedItem.IsInitialized);

            MenuState currentState = MenuState;
            currentState.TopSelectedItem = topSelectedItem;
            currentState.SubSelectedItem = subSelectedItem;
            menuState = currentState;
        }

        public void InvokeSelectedAction(HashSet<DistributedId> affectedObjects)
        {
            MenuState state = MenuState;
            if (!state.TopSelectedItem.IsInitialized)
            {
                // was nothing to do
                return;
            }

            // ok top item is known initialized. get its structure entry
            Action<HashSet<DistributedId>> action = menuStructure.Action(state.TopSelectedItem);

            if (state.SubSelectedItem.IsInitialized)
            {
                MenuStructure childStructure = menuStructure.Child(state.TopSelectedItem);
                action = childStructure.Action(state.SubSelectedItem);
            }

            if (action != null)
            {
                action(affectedObjects);
            }
            else
            {
                HoloDebug.Warn($"LocalMenu.InvokeSelectedActiono: Did not find action for menu state {state}");
            }
        }

        public void OnDelete()
        {
            HoloDebug.Log($"LocalMenu.OnDelete: Deleting {DistributedObject.Id}");
            // and we blow ourselves awaaaay
            Destroy(this.gameObject);
        }

        #endregion

        #region MonoBehaviour

        public void Start()
        {
            if (DistributedViewpoint.Instance != null)
            {
                Matrix4x4 viewpointToLocalMatrix = DistributedViewpoint.Instance.ViewpointToLocalMatrix();
                Vector3 localPosition = viewpointToLocalMatrix.MultiplyPoint(MenuState.ViewpointPosition);
                Vector3 localForwardDirection = viewpointToLocalMatrix.MultiplyVector(MenuState.ViewpointForwardDirection);
                // what if these are not orthogonal? let's try
                Quaternion localOrientation = Quaternion.LookRotation(localForwardDirection, Vector3.up);

                transform.localPosition = localPosition;
                transform.localRotation = localOrientation;
            }
        }

        /// <summary>
        /// Update the current menu and possibly submenu(s) to match current selection state.
        /// </summary>
        /// <remarks>
        /// This method may update the menuLevels list.
        /// </remarks>
        public void Update()
        {
            if (priorMenuState.SubSelectedItem != MenuState.SubSelectedItem)
            {
                if (priorMenuState.SubSelectedItem.IsInitialized)
                {
                    menuLevels[1].ColorizeMenuItem(priorMenuState.SubSelectedItem, Color.grey);
                }
            }

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

            }

            if (priorMenuState.TopSelectedItem.IsInitialized)
            {
                menuLevels[0].ColorizeMenuItem(priorMenuState.TopSelectedItem, Color.grey);
            }

            if (MenuState.TopSelectedItem.IsInitialized)
            {
                if (menuLevels.Count == 1)
                {
                    // maybe need to create submenu?
                    MenuStructure childMenuStructure = menuStructure.Child(menuState.TopSelectedItem);

                    if (childMenuStructure != null && childMenuStructure.Count > 0)
                    {
                        Vector3 parentLocalPosition = menuLevels[0].GetRelativePosition(
                            Vector3.zero, MenuState.TopSelectedItem.AsIndex);
                        menuLevels.Add(new MenuLevel(this, parentLocalPosition, 1, childMenuStructure));
                    }
                }

                // is there a selected subitem? if so, highlight it

                if (MenuState.SubSelectedItem.IsInitialized)
                {
                    menuLevels[1].ColorizeMenuItem(MenuState.SubSelectedItem, Color.white);
                }
                else
                {
                    menuLevels[0].ColorizeMenuItem(MenuState.TopSelectedItem, Color.white);
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
            float smallestDistance = float.MaxValue;
            int closestMenuItemIndex = -1;

            for (int i = 0; i < menuLevels.Count; i++)
            {
                MenuLevel menuLevel = menuLevels[i];
                //_logBuffer.Append($"Checking menu #{i}...{Environment.NewLine}");
                Option<(float, int)> result = menuLevel.GetClosestMenuItemIfAny(handPosition);

                if (result.HasValue)
                {
                    if (result.Value.Item1 < smallestDistance)
                    {
                        smallestDistance = result.Value.Item1;
                        closestMenuIndex = i;
                        closestMenuItemIndex = result.Value.Item2;
                    }
                }
            }

            if (closestMenuIndex == -1)
            {
                return Option<(int, MenuItemId)>.None;
            }
            else
            {
                return (closestMenuIndex, closestMenuItemIndex + 1);
            }
        }

        #endregion
    }
}
