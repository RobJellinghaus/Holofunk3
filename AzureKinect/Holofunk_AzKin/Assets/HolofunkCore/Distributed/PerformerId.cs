// Copyright (c) 2021 by Rob Jellinghaus.

using Distributed.State;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace Holofunk.Distributed
{
    /// <summary>
    /// The identifier of a performer.
    /// </summary>
    public struct PerformerId : INetSerializable
    {
        private byte value;

        public PerformerId(byte value)
        {
            // ID 0 is not valid, reserved for uninitialized value
            Contract.Requires(value >= 1);
            Contract.Requires(value <= byte.MaxValue);

            this.value = value;
        }

        public bool IsInitialized => value > 0;

        public static implicit operator PerformerId(int value) => new PerformerId((byte)value);

        public static explicit operator byte(PerformerId id) => id.value;

        public override string ToString()
        {
            return $"#{value}";
        }

        public static bool operator ==(PerformerId left, PerformerId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PerformerId left, PerformerId right)
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
            return obj is PerformerId id &&
                   value == id.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }
    }
}
