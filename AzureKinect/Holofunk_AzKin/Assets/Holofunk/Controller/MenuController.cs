/// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Controller;
using Holofunk.Core;
using Holofunk.Menu;
using Holofunk.Perform;
using UnityEngine;

namespace Holofunk.Controller
{
    /// <summary>
    /// Component which manages the interaction between a PPlusController and a LocalMenu, possibly
    /// selecting a different menu item.
    /// </summary>
    /// <remarks>
    /// The LocalMenu component manages rendering of the menu; this component manages the interaction.
    /// This component exists in the menu prototype in the HL2 application.
    /// </remarks>
    public class MenuController : MonoBehaviour
    {
        /// <summary>
        /// The hand manipulating this menu.
        /// </summary>
        PPlusController _joyconController;

        /// <summary>
        /// The menu this controller controls is expected to be a sibling component in the same menu
        /// prototype game object.
        /// </summary>
        private DistributedMenu Menu => GetComponent<DistributedMenu>();

        /// <summary>
        /// Initialize a newly instantiated MenuController.
        /// </summary>
        /// <param name="pplusController">The controller which controls this menu.</param>
        public void Initialize(PPlusController pplusController)
        {
            Contract.Assert(Menu != null);

            _joyconController = pplusController;
        }

        void Update()
        {
            // Only update if the controller is a thing.
            if (!_joyconController.IsUpdatable())
            {
                return;
            }

            Vector3 handPosition = _joyconController.GetViewpointHandPosition();

            // Find the closest item.
            Option<(int, MenuItemId)> closestItem = ((LocalMenu)Menu.LocalObject).GetClosestMenuItemIfAny(handPosition);

            if (!closestItem.HasValue)
            {
                // no menu items were in range at all.  do nothing.  user can gesture to exit current menus
                //_logBuffer.Append($"No menu items in range whatsoever.{Environment.NewLine}");
                //GUIController.Instance.Text2 = _logBuffer.ToString();
                HoloDebug.Log("No closest menu item");
                return;
            }

            // OK, so we have a closest menu item after all.
            // If the depth of the closest item is 0, then the sub-menu is now unselected;
            // if the depth of the closest item is 1, then the sub-menu is still selected.
            MenuItemId priorTopSelectedItem = Menu.MenuState.TopSelectedItem;
            MenuItemId priorSubSelectedItem = Menu.MenuState.SubSelectedItem;

            MenuItemId newTopSelectedItem = closestItem.Value.Item1 == 0 ? closestItem.Value.Item2 : priorTopSelectedItem;
            MenuItemId newSubSelectedItem = closestItem.Value.Item1 == 0 ? default(MenuItemId) : closestItem.Value.Item2;

            // If top menu not selected, bottom menu can't be selected
            Contract.Assert(newTopSelectedItem.IsInitialized ? true : !priorSubSelectedItem.IsInitialized);            

            if (priorTopSelectedItem != newTopSelectedItem
                || priorSubSelectedItem != newSubSelectedItem)
            {
                Menu.SetSelection(newTopSelectedItem, newSubSelectedItem);
            }
        }
    }
}
