// Copyright (c) 2021 by Rob Jellinghaus.

using Distributed.State;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace Holofunk.Distributed
{
    /// <summary>
    /// The identifier of a user from the Kinect's point of view.
    /// </summary>
    public struct UserId : INetSerializable
    {
        private ulong value;

        public UserId(ulong value)
        {
            this.value = value;
        }

        public static implicit operator UserId(ulong value) => new UserId(value);

        public static explicit operator ulong(UserId id) => id.value;

        public static bool operator ==(UserId left, UserId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UserId left, UserId right)
        {
            return !(left == right);
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(value);
        }

        public void Deserialize(NetDataReader reader)
        {
            value = reader.GetULong();
        }

        public override bool Equals(object obj)
        {
            return obj is UserId id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}
