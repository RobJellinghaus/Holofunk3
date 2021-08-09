// Copyright (c) 2020 by Rob Jellinghaus.

using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;

namespace Distributed.State
{
    /// <summary>
    /// ID of a distributed object.
    /// </summary>
    /// <remarks>
    /// We believe strongly in not using C# primitive types for semantically meaningful values, due to type confusion.
    /// </remarks>
    public struct DistributedId
    {
        public class Comparer : IComparer<DistributedId>
        {
            public int Compare(DistributedId x, DistributedId y)
            {
                return x.Value.CompareTo(y.Value);
            }
            public static Comparer Instance = new Comparer();
        }

        public readonly uint Value;

        public DistributedId(uint value)
        {
            if (value == 0)
            {
                throw new ArgumentException("DistributedId value 0 is not valid (reserved for default/uninitialized)");
            }

            this.Value = value;
        }

        public bool IsInitialized => Value > 0;

        public static implicit operator DistributedId(uint value) => new DistributedId((byte)value);

        public override string ToString()
        {
            return $"#{Value}";
        }

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType(Serialize, Deserialize);
        }

        public static void Serialize(NetDataWriter writer, DistributedId id)
        {
            writer.Put(id.Value);
        }

        public static DistributedId Deserialize(NetDataReader reader)
        {
            return reader.GetUInt();
        }

        public static bool operator ==(DistributedId left, DistributedId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DistributedId left, DistributedId right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is DistributedId id &&
                   Value == id.Value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + Value.GetHashCode();
        }
    }
}
