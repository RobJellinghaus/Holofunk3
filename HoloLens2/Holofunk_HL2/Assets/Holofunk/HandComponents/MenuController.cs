/// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Core;
using Holofunk.Menu;
using Holofunk.Perform;
using UnityEngine;

namespace Holofunk.HandComponents
{
    /// <summary>
    /// Component which manages the interaction between a HandController and a LocalMenu, possibly
    /// selecting a different menu item.
    /// </summary>
    /// <remarks>
    /// The LocalMenu component manages rendering of the menu; this component manages the interaction.
    /// </remarks>
    public class MenuController : MonoBehaviour
    {
        /// <summary>
        /// The hand manipulating this menu.
        /// </summary>
        HandController _handController;

        /// <summary>
        /// The menu.
        /// </summary>
        DistributedMenu _menu;

        /// <summary>
        /// Initialize a newly instantiated MenuController.
        /// </summary>
        /// <param name="menuModel">The base model that will be passed to the menu item actions.</param>
        /// <param name="playerHandModel">The player hand model which this menu will use for tracking interaction.</param>
        /// <param name="rootPosition">The world space position the menu tree is being popped up at.</param>
        /// <param name="rootRelativePosition">The position of this (possibly multiply nested) child relative to
        /// the root of the whole menu tree; None if this is the root menu.</param>
        public void Initialize(
            HandController handController,
            DistributedMenu menu)
        {
            _handController = handController;
            _menu = menu;
        }

        void Update()
        {
            // first, find the hand 
            PerformerState performer = _handController.DistributedPerformer.GetPerformer();
            Vector3 handPosition = _handController.HandPosition(ref performer);

            // First, find the closest item.
            Option<(int, MenuItemId)> closestItem = ((LocalMenu)_menu.LocalObject).GetClosestMenuItemIfAny(handPosition);

            if (!closestItem.HasValue)
            {
                // no menu items were in range at all.  do nothing.  user can gesture to exit current menus
                //_logBuffer.Append($"No menu items in range whatsoever.{Environment.NewLine}");
                //GUIController.Instance.Text2 = _logBuffer.ToString();
                return;
            }

            // OK, so we have a closest menu item after all.
            // If the depth of the closest item is 0, then the sub-menu is now unselected;
            // if the depth of the closest item is 1, then the sub-menu is still selected.
            MenuItemId priorTopSelectedItem = _menu.MenuState.TopSelectedItem;
            MenuItemId priorSubSelectedItem = _menu.MenuState.SubSelectedItem;

            MenuItemId newTopSelectedItem = closestItem.Value.Item1 == 0 ? closestItem.Value.Item2 : priorTopSelectedItem;
            MenuItemId newSubSelectedItem = closestItem.Value.Item1 == 0 ? default(MenuItemId) : closestItem.Value.Item2;

            // If top menu not selected, bottom menu can't be selected
            Contract.Assert(newTopSelectedItem.IsInitialized ? true : !priorSubSelectedItem.IsInitialized);            

            if (priorTopSelectedItem != newTopSelectedItem
                || priorSubSelectedItem != newSubSelectedItem)
            {
                _menu.SetSelection(newTopSelectedItem, newSubSelectedItem);
            }
        }
    }
}
