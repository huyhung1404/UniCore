using System;
using System.Security.Cryptography;

namespace UniCore.Storage
{
    public enum EncryptionType
    {
        None,
        AES,
        Custom
    }

    public interface IEncryptor
    {
        public byte[] Encrypt(byte[] data);
        public byte[] Decrypt(byte[] data);
    }

    public class NoEncryptor : IEncryptor
    {
        public byte[] Encrypt(byte[] data) => data;
        public byte[] Decrypt(byte[] data) => data;
    }

    public class AESEncryptor : IEncryptor
    {
        public byte[] Encrypt(byte[] data)
        {
            if (data == null || data.Length == 0) return data;

            using var aes = Aes.Create();
            aes.Key = StorageSystem.GetKey();
            aes.GenerateIV();

            using var enc = aes.CreateEncryptor();
            var cipherText = enc.TransformFinalBlock(data, 0, data.Length);
            
            var result = new byte[16 + cipherText.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, 16);
            Buffer.BlockCopy(cipherText, 0, result, 16, cipherText.Length);
            
            return result;
        }

        public byte[] Decrypt(byte[] data)
        {
            if (data == null || data.Length <= 16) return data;

            using var aes = Aes.Create();
            aes.Key = StorageSystem.GetKey();
            
            var iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;

            using var dec = aes.CreateDecryptor();
            return dec.TransformFinalBlock(data, 16, data.Length - 16);
        }
    }
}