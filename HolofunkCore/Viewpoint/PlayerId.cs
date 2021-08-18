// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib.Utils;

namespace Holofunk.Viewpoint
{
    /// <summary>
    /// The 1-based identifier of a player in a Viewpoint.
    /// </summary>
    public struct PlayerId
    {
        private byte value;

        public PlayerId(byte value)
        {
            // ID 0 is not valid, reserved for uninitialized value
            Contract.Requires(value >= 1);
            Contract.Requires(value <= byte.MaxValue);

            this.value = value;
        }

        public bool IsInitialized => value > 0;

        public static implicit operator PlayerId(byte value) => new PlayerId(value);

        public static explicit operator byte(PlayerId id) => id.value;

        public override string ToString() => $"#{value}";

        public static bool operator ==(PlayerId left, PlayerId right) => left.Equals(right);

        public static bool operator !=(PlayerId left, PlayerId right) => !(left == right);

        public static void Serialize(NetDataWriter writer, PlayerId playerId) => writer.Put(playerId.value);

        public static PlayerId Deserialize(NetDataReader reader) => new PlayerId(reader.GetByte());

        public override bool Equals(object obj) => obj is PlayerId id && value == id.value;

        public override int GetHashCode() =>  -1584136870 + value.GetHashCode();
    }
}
