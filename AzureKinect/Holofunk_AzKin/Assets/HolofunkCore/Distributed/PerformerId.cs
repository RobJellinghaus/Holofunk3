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
    public struct PerformerId
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

        public override string ToString() => $"#{value}";

        public static bool operator ==(PerformerId left, PerformerId right) => left.Equals(right);

        public static bool operator !=(PerformerId left, PerformerId right) => !(left == right);

        public static void Serialize(NetDataWriter writer, PerformerId id) => writer.Put(id.value);

        public static PerformerId Deserialize(NetDataReader reader) => new PerformerId(reader.GetByte());

        public override bool Equals(object obj) => obj is PerformerId id && value == id.value;

        public override int GetHashCode() =>  -1584136870 + value.GetHashCode();
    }
}
