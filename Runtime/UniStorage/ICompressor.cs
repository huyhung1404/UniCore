using System.IO;
using System.IO.Compression;

namespace UniCore.Storage
{
    public enum CompressionType
    {
        None,
        GZip,
        Custom
    }

    public interface ICompressor
    {
        public byte[] Compress(byte[] data);
        public byte[] Decompress(byte[] data);
    }

    public class NoCompressor : ICompressor
    {
        public byte[] Compress(byte[] data) => data;
        public byte[] Decompress(byte[] data) => data;
    }

    public class GZipCompressor : ICompressor
    {
        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            using var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(data, 0, data.Length);
            }

            return ms.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            using var inputMs = new MemoryStream(data);
            using var gzip = new GZipStream(inputMs, CompressionMode.Decompress);
            using var outputMs = new MemoryStream();

            gzip.CopyTo(outputMs);
            return outputMs.ToArray();
        }
    }
}