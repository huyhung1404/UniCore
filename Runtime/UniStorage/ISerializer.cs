using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;

namespace UniCore.Storage
{
    public enum SerializationType
    {
        Json,
        Binary,
        Custom
    }

    public interface ISerializer
    {
        public byte[] Serialize(object obj);
        public T Deserialize<T>(byte[] data);
    }

    public class JsonSerializer : ISerializer
    {
        public byte[] Serialize(object obj)
        {
            if (obj == null) return Array.Empty<byte>();
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;

            using var ms = new MemoryStream(data);
            using var sr = new StreamReader(ms, Encoding.UTF8);
            using var jr = new JsonTextReader(sr);

            var serializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
            return serializer.Deserialize<T>(jr);
        }
    }

    public class BinarySerializer : ISerializer
    {
        [Obsolete(
            "BinaryFormatter is obsolete and strictly prohibited in modern .NET due to critical security risks. Consider migrating to MessagePack or Protobuf for IL2CPP optimization.")]
        public byte[] Serialize(object obj)
        {
            if (obj == null) return Array.Empty<byte>();

            using var ms = new MemoryStream();
#pragma warning disable SYSLIB0011
            var bf = new BinaryFormatter();
            bf.Serialize(ms, obj);
#pragma warning restore SYSLIB0011
            return ms.ToArray();
        }

        [Obsolete(
            "BinaryFormatter is obsolete and strictly prohibited in modern .NET due to critical security risks. Consider migrating to MessagePack or Protobuf for IL2CPP optimization.")]
        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default;

            using var ms = new MemoryStream(data);
#pragma warning disable SYSLIB0011
            var bf = new BinaryFormatter();
            return (T)bf.Deserialize(ms);
#pragma warning restore SYSLIB0011
        }
    }
}