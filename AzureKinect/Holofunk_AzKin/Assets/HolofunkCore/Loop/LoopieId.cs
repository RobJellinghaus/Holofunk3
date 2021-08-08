// Copyright by Rob Jellinghaus. All rights reserved.

using Distributed.State;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

namespace Holofunk.Loop
{
    /// <summary>
    /// The 1-based identifier of a loopie.
    /// </summary>
    public struct LoopieId
    {
        private int value;

        public LoopieId(int value)
        {
            // ID 0 is not valid, reserved for uninitialized value
            Contract.Requires(value >= 1);
            Contract.Requires(value <= int.MaxValue);

            this.value = value;
        }

        public bool IsInitialized => value > 0;

        public static implicit operator LoopieId(int value) => new LoopieId(value);

        public static explicit operator int(LoopieId id) => id.value;

        public override string ToString() => $"#{value}";

        public static bool operator ==(LoopieId left, LoopieId right) => left.Equals(right);

        public static bool operator !=(LoopieId left, LoopieId right) => !(left == right);

        public static void Serialize(NetDataWriter writer, LoopieId loopieId) => writer.Put(loopieId.value);

        public static LoopieId Deserialize(NetDataReader reader) => new LoopieId(reader.GetByte());

        public override bool Equals(object obj) => obj is LoopieId id && value == id.value;

        public override int GetHashCode() =>  -1584136870 + value.GetHashCode();
    }
}
