// Copyright by Rob Jellinghaus. All rights reserved.

using Holofunk.Distributed;
using Holofunk.Sound;
using LiteNetLib.Utils;
using UnityEngine;

namespace Holofunk.Menu
{
    /// <summary>
    /// Serialized state of a popup menu.
    /// </summary>
    public struct MenuState : INetSerializable
    {
        /// <summary>
        /// Which kind of popup menu is this?
        /// </summary>
        public MenuKind MenuKind { get; set; }

        /// <summary>
        /// The ID of the sub-level selected menu item (if any).
        /// </summary>
        /// <remarks>
        /// If this is initialized, TopSelectedItem must be initialized as well (e.g. a sub-level item can't be
        /// selected unless there is already a top-level selected item).
        /// </remarks>
        public MenuItemId SubSelectedItem { get; set; }

        /// <summary>
        /// The ID of the top-level selected menu item (if any).
        /// </summary>
        public MenuItemId TopSelectedItem { get; set; }

        /// <summary>
        /// The forward direction of the popup menu.
        /// </summary>
        /// <remarks>
        /// The menu's up direction is always this crossed with the Y axis, since the Y axis is always upwards.
        /// </remarks>
        public Vector3 ViewpointForwardDirection { get; set; }

        /// <summary>
        /// The position of the menu, in viewpoint coordinates.
        /// </summary>
        public Vector3 ViewpointPosition { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            MenuKind = MenuKind.Deserialize(reader);
            SubSelectedItem = MenuItemId.Deserialize(reader);
            TopSelectedItem = MenuItemId.Deserialize(reader);
            ViewpointForwardDirection = reader.GetVector3();
            ViewpointPosition = reader.GetVector3();
        }

        public void Serialize(NetDataWriter writer)
        {
            MenuKind.Serialize(writer, MenuKind);
            MenuItemId.Serialize(writer, SubSelectedItem);
            MenuItemId.Serialize(writer, TopSelectedItem);
            writer.Put(ViewpointForwardDirection);
            writer.Put(ViewpointPosition);
        }

        public override string ToString() => $"MenuState[{MenuKind}, @{ViewpointPosition}, =>{ViewpointForwardDirection} top({TopSelectedItem.Value}) sub({TopSelectedItem.Value})]";
    }
}
