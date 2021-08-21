// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;

namespace Holofunk.Menu
{
    /// <summary>
    /// The varieties of popup menu.
    /// </summary>
    public enum MenuKinds
    {
        /// <summary>
        /// The empty value.
        /// </summary>
        Uninitialized,

        /// <summary>
        /// The system menu.
        /// </summary>
        System,

        /// <summary>
        /// The sound effects menu.
        /// </summary>
        SoundEffects,
    }

    /// <summary>
    /// Serializable struct that represents which kind of menu this is.
    /// </summary>
    public struct MenuKind
    {
        private MenuKinds value;

        public MenuKinds Value => value;

        public MenuKind(MenuKinds value)
        {
            this.value = value;
        }

        public bool IsInitialized => value > MenuKinds.Uninitialized;

        public static implicit operator MenuKind(MenuKinds value) => new MenuKind(value);

        public override string ToString() => $"#{value}";

        public static bool operator ==(MenuKind left, MenuKind right) => left.Equals(right);

        public static bool operator !=(MenuKind left, MenuKind right) => !(left == right);

        public static void Serialize(NetDataWriter writer, MenuKind MenuKind) => writer.Put((int)MenuKind.value);

        public static MenuKind Deserialize(NetDataReader reader) => new MenuKind((MenuKinds)reader.GetInt());

        public override bool Equals(object obj) => obj is MenuKind id && value == id.value;

        public override int GetHashCode() => -1584136870 + value.GetHashCode();

    }
}
