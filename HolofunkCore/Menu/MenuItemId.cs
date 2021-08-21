// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib.Utils;

namespace Holofunk.Menu
{
    /// <summary>
    /// The 1-based identifier of a MenuItem.
    /// </summary>
    public struct MenuItemId
    {
        private int value;

        public MenuItemId(int value)
        {
            // ID 0 is not valid, reserved for uninitialized value.
            // If you must have it, you can use default(MenuItemId)
            Contract.Requires(value >= 1);
            Contract.Requires(value <= int.MaxValue);

            this.value = value;
        }

        public bool IsInitialized => value > 0;

        public static implicit operator MenuItemId(int value) => new MenuItemId(value);

        public int Value => value;

        /// <summary>
        /// Get this ID as a zero-based index.
        /// </summary>
        public int AsIndex
        {
            get
            {
                Core.Contract.Assert(IsInitialized);
                return value - 1;
            }
        }

        public override string ToString() => $"#{value}";

        public static bool operator ==(MenuItemId left, MenuItemId right) => left.Equals(right);

        public static bool operator !=(MenuItemId left, MenuItemId right) => !(left == right);

        public static void Serialize(NetDataWriter writer, MenuItemId MenuItemId) => writer.Put(MenuItemId.value);

        public static MenuItemId Deserialize(NetDataReader reader) => new MenuItemId(reader.GetInt());

        public override bool Equals(object obj) => obj is MenuItemId id && value == id.value;

        public override int GetHashCode() => -1584136870 + value.GetHashCode();
    }
}
