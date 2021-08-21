// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using Holofunk.Core;
using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib;
using UnityEngine;
using static Holofunk.Menu.MenuMessages;

namespace Holofunk.Menu
{
    /// <summary>
    /// The distributed interface of a Menu.
    /// </summary>
    /// <remarks>
    /// The initial model uses creator-owned Menus. TBD whether we wind up wanting
    /// viewpoint-owned Menus (for better experience if creators disconnect and reconnect).
    /// </remarks>
    public class DistributedMenu : DistributedComponent, IDistributedMenu
    {
        #region MonoBehaviours

        public void Start()
        {
            // If we have no ID yet, then we are an owning object that has not yet gotten an initial ID.
            // So, initialize ourselves as an owner.
            if (!Id.IsInitialized)
            {
                InitializeOwner();
            }
        }

        #endregion

        #region IDistributedMenu

        public IDistributedObject DistributedObject => gameObject.GetComponent<DistributedMenu>();

        public override ILocalObject LocalObject => GetLocalMenu();

        private LocalMenu GetLocalMenu() => gameObject == null ? null : gameObject.GetComponent<LocalMenu>();

        /// <summary>
        /// Get the Menu state.
        /// </summary>
        public MenuState MenuState => GetLocalMenu().MenuState;

        public void SetSelection(MenuItemId topSelectedItemId, MenuItemId subSelectedItemId) =>
            RouteReliableMessage(isRequest => new SetSelected(Id, isRequest: !IsOwner, topSelectedItemId, subSelectedItemId));

        public void InvokeSelectedAction() => GetLocalMenu().InvokeSelectedAction();

        #endregion

        #region DistributedState

        public override void OnDelete()
        {
            HoloDebug.Log($"DistributedMenu.OnDelete: Deleting {Id}");
            // propagate locally
            GetLocalMenu().OnDelete();
        }

        protected override void SendCreateMessage(NetPeer netPeer)
        {
            HoloDebug.Log($"DistributedMenu.SendCreateMessage: Sending Menu.Create for id {Id} to peer {netPeer.EndPoint} with Menu {MenuState}");
            Host.SendReliableMessage(new Create(Id, MenuState), netPeer);
        }

        protected override void SendDeleteMessage(NetPeer netPeer, bool isRequest)
        {
            HoloDebug.Log($"DistributedMenu.SendDeleteMessage: Sending Menu.Delete for id {Id} to peer {netPeer.EndPoint} with Menu {MenuState}");
            Host.SendReliableMessage(new Delete(Id, isRequest), netPeer);
        }

        #endregion

        #region Instantiation

        /// <summary>
        /// Create a new Menu at this position in viewpoint space.
        /// </summary>
        /// <remarks>
        /// This is how Menus come to exist on their owning hosts.
        /// 
        /// TODO: figure out how to refactor out the shared plumbing here, similarly to the Registrar.
        /// </remarks>
        public static GameObject Create(
            MenuKind kind,
            Vector3 viewpointForwardDirection,
            Vector3 viewpointPosition)
        {
            GameObject prototypeMenu = DistributedObjectFactory.FindPrototypeContainer(
                DistributedObjectFactory.DistributedType.Menu);
            GameObject localContainer = DistributedObjectFactory.FindLocalhostInstanceContainer(
                DistributedObjectFactory.DistributedType.Menu);

            GameObject newMenu = Instantiate(prototypeMenu, localContainer.transform);
            newMenu.SetActive(true);
            DistributedMenu distributedMenu = newMenu.GetComponent<DistributedMenu>();
            LocalMenu localMenu = distributedMenu.GetLocalMenu();

            // First set up the Menu state in distributed terms.
            localMenu.Initialize(new MenuState
            {
                MenuKind = kind,
                SubSelectedItem = default(MenuItemId),
                TopSelectedItem = default(MenuItemId),
                ViewpointForwardDirection = viewpointForwardDirection,
                ViewpointPosition = viewpointPosition,
            });

            // Then enable the distributed behavior.
            distributedMenu.InitializeOwner();

            // And finally set the Menu name.
            newMenu.name = $"{distributedMenu.Id}";

            return newMenu;
        }

        #endregion
    }
}
