// Copyright by Rob Jellinghaus. All rights reserved.

using LiteNetLib.Utils;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// The identifier of a user from the Kinect's point of view.
    /// </summary>
    public struct UserId
    {
        private ulong value;

        public UserId(ulong value)
        {
            this.value = value;
        }

        public static implicit operator UserId(ulong value) => new UserId(value);

        public static explicit operator ulong(UserId id) => id.value;

        public static bool operator ==(UserId left, UserId right) => left.Equals(right);

        public static bool operator !=(UserId left, UserId right) => !(left == right);

        public static void Serialize(NetDataWriter writer, UserId id) => writer.Put(id.value);

        public static UserId Deserialize(NetDataReader reader) => new UserId(reader.GetULong());

        public override bool Equals(object obj) => obj is UserId id && value == id.value;

        public override int GetHashCode() => -1584136870 + value.GetHashCode();
    }
}
