using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace UniCore.Storage
{
    public enum StorageType
    {
        LocalStorage,
        PlayerPrefs,
        Custom
    }

    public interface IStorageProvider
    {
        public void Save(string fileName, byte[] data);
        public byte[] Load(string fileName);

        public Task SaveAsync(string fileName, byte[] data);
        public Task<byte[]> LoadAsync(string fileName);
    }

    public class PlayerPrefsStorage : IStorageProvider
    {
        private static string Key(string fileName) => $"storage_{fileName}";

        public void Save(string fileName, byte[] data)
        {
            if (data == null || data.Length == 0) return;

            var base64 = Convert.ToBase64String(data);
            PlayerPrefs.SetString(Key(fileName), base64);
            PlayerPrefs.Save();
        }

        public async Task SaveAsync(string fileName, byte[] data)
        {
            if (data == null || data.Length == 0) return;
            var base64 = await Task.Run(() => Convert.ToBase64String(data));
            PlayerPrefs.SetString(Key(fileName), base64);
            PlayerPrefs.Save();
        }

        public byte[] Load(string fileName)
        {
            if (!PlayerPrefs.HasKey(Key(fileName))) return null;
            var base64 = PlayerPrefs.GetString(Key(fileName));
            return Convert.FromBase64String(base64);
        }

        public async Task<byte[]> LoadAsync(string fileName)
        {
            if (!PlayerPrefs.HasKey(Key(fileName))) return null;
            var base64 = PlayerPrefs.GetString(Key(fileName));
            return await Task.Run(() => Convert.FromBase64String(base64));
        }
    }

    public class LocalStorage : IStorageProvider
    {
        private static string GetPath(string fileName) => Path.Combine(Application.persistentDataPath, $"{fileName}.dat");
        private static string GetTempPath(string fileName) => Path.Combine(Application.persistentDataPath, $"{fileName}.temp");
        private static string GetBakPath(string fileName) => Path.Combine(Application.persistentDataPath, $"{fileName}.bak");

        public void Save(string fileName, byte[] data)
        {
            var path = GetPath(fileName);
            var temp = GetTempPath(fileName);
            var bak = GetBakPath(fileName);

            ProcessSafeSave(path, temp, bak, data);
        }

        public async Task SaveAsync(string fileName, byte[] data)
        {
            var path = GetPath(fileName);
            var temp = GetTempPath(fileName);
            var bak = GetBakPath(fileName);

            await ProcessSafeSaveAsync(path, temp, bak, data);
        }

        public byte[] Load(string fileName)
        {
            var path = GetPath(fileName);
            var bak = GetBakPath(fileName);

            if (File.Exists(path))
            {
                try
                {
                    return File.ReadAllBytes(path);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LocalStorage] Failed to read main file: {e.Message}. Attempting backup.");
                }
            }

            if (File.Exists(bak))
            {
                try
                {
                    return File.ReadAllBytes(bak);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LocalStorage] Failed to read backup file: {e.Message}");
                }
            }

            return null;
        }

        public async Task<byte[]> LoadAsync(string fileName)
        {
            var path = GetPath(fileName);
            var bak = GetBakPath(fileName);

            if (File.Exists(path))
            {
                try
                {
                    return await File.ReadAllBytesAsync(path);
                }
                catch (Exception)
                {
                    /* Fallback to backup */
                }
            }

            if (File.Exists(bak))
            {
                try
                {
                    return await File.ReadAllBytesAsync(bak);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        private void ProcessSafeSave(string path, string temp, string bak, byte[] data)
        {
            File.WriteAllBytes(temp, data);

            if (File.Exists(path))
            {
                File.Copy(path, bak, true);
                File.Delete(path);
            }

            File.Move(temp, path);
        }

        private async Task ProcessSafeSaveAsync(string path, string temp, string bak, byte[] data)
        {
            await File.WriteAllBytesAsync(temp, data);

            if (File.Exists(path))
            {
                File.Copy(path, bak, true);
                File.Delete(path);
            }

            File.Move(temp, path);
        }
    }
}