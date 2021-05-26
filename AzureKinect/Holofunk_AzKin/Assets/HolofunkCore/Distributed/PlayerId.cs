// Copyright (c) 2021 by Rob Jellinghaus.

using Distributed.State;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace Holofunk.Distributed
{
    /// <summary>
    /// The identifier of a player from an Audience point of view.
    /// </summary>
    public struct PlayerId : INetSerializable
    {
        private byte value;

        public PlayerId(byte value)
        {
            Contract.Requires(value >= 0);
            Contract.Requires(value <= byte.MaxValue);

            this.value = value;
        }

        public static implicit operator PlayerId(int value) => new PlayerId((byte)value);

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
