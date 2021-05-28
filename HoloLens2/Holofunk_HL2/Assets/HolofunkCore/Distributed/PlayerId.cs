// Copyright (c) 2021 by Rob Jellinghaus.

using Distributed.State;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace Holofunk.Distributed
{
    /// <summary>
    /// The identifier of a player in a Viewpoint.
    /// </summary>
    public struct PlayerId : INetSerializable
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

        public static bool operator ==(PlayerId left, PlayerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerId left, PlayerId right)
        {
            return !(left == right);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(value);
        }

        public void Deserialize(NetDataReader reader)
        {
            value = reader.GetByte();
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerId id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}
