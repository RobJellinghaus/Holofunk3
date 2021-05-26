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

        public static void RegisterWith(NetPacketProcessor packetProcessor)
        {
            packetProcessor.RegisterNestedType<PlayerId>();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(value);
        }

        public void Deserialize(NetDataReader reader)
        {
            value = reader.GetByte();
        }
    }
}
