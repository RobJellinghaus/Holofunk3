// Copyright (c) 2021 by Rob Jellinghaus.

using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Holofunk.Distributed
{
    public static class SerializationExtensions
    {
        public static void Put(this NetDataWriter writer, Vector3 value)
        {
            writer.Put(value[0]);
            writer.Put(value[1]);
            writer.Put(value[2]);
        }

        public static Vector3 GetVector3(this NetDataReader reader)
        {
            return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public static void Put(this NetDataWriter writer, Vector4 value)
        {
            writer.Put(value[0]);
            writer.Put(value[1]);
            writer.Put(value[2]);
            writer.Put(value[3]);
        }

        public static Vector4 GetVector4(this NetDataReader reader)
        {
            return new Vector4(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        public static void Put(this NetDataWriter writer, Matrix4x4 value)
        {
            writer.Put(value.GetColumn(0));
            writer.Put(value.GetColumn(1));
            writer.Put(value.GetColumn(2));
            writer.Put(value.GetColumn(3));
        }

        public static Matrix4x4 GetMatrix4x4(this NetDataReader reader)
        {
            return new Matrix4x4(reader.GetVector4(), reader.GetVector4(), reader.GetVector4(), reader.GetVector4());
        }
    }
}
