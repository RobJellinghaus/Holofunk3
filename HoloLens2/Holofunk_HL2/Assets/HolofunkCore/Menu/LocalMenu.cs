// Copyright by Rob Jellinghaus. All rights reserved.

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

        #endregion

        #region IDistributedMenu

        public void Initialize(MenuState menuState)
        {
            this.menuState = menuState;
        }

        public MenuState MenuState => menuState;

        public IDistributedObject DistributedObject => throw new NotImplementedException();

        public void SetSelection(MenuItemId topSelectedItem, MenuItemId subSelectedItem)
        {
            MenuState currentState = MenuState;
            currentState.TopSelectedItem = topSelectedItem;
            currentState.SubSelectedItem = subSelectedItem;
            menuState = currentState;
        }

        public void OnDelete()
        {
            // nothing to do... gameobject deletion is sufficient
        }

        #endregion

        #region MonoBehaviour

        public void Start()
        {
            // TODO: create all the right structure & objects
        }

        private void InstantiateMenuObjects()
        {
        }

        public void Update()
        {
        }

        #endregion
    }
}
