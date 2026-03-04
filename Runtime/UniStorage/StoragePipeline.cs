using System.Threading.Tasks;
using UnityEngine;

namespace UniCore.Storage
{
    public class StoragePipeline
    {
        public byte[] Key { get; private set; }
        private ISerializer _serializer;
        private ICompressor _compressor;
        private IEncryptor _encryptor;
        private IProtector _protector;
        private IStorageProvider _storage;
        private readonly int _version;

        public StoragePipeline(ISettings settings)
        {
            if (settings == null)
            {
                LoadSettingDefault();
                return;
            }

            Key = settings.Key.GetKey();
            _serializer = settings.Serializer;
            _compressor = settings.Compressor;
            _encryptor = settings.Encryptor;
            _protector = settings.Protector;
            _storage = settings.StorageProvider;
            _version = settings.Version;
        }

        private static string GetVersionKey(string fileName) => $"storage_version_{fileName}";

        private void LoadSettingDefault()
        {
            Key = null;
            _serializer = new JsonSerializer();
            _compressor = new NoCompressor();
            _encryptor = new NoEncryptor();
            _protector = new NoProtector();
            _storage = new LocalStorage();
        }

        public void Save<T>(string fileName, T data)
        {
            var bytes = Pack(data);
            _storage.Save(fileName, bytes);
            
            PlayerPrefs.SetInt(GetVersionKey(fileName), _version);
            PlayerPrefs.Save();
        }
        
        public async Task SaveAsync<T>(string fileName, T data)
        {
            var bytes = await Task.Run(() => Pack(data)); 
            await _storage.SaveAsync(fileName, bytes); 
            
            PlayerPrefs.SetInt(GetVersionKey(fileName), _version);
            PlayerPrefs.Save();
        }

        public T Load<T>(string fileName)
        {
            var bytes = _storage.Load(fileName);
            if (bytes == null) return default;
            
            var currentVersion = PlayerPrefs.GetInt(GetVersionKey(fileName), _version);
            return Unpack<T>(bytes, currentVersion); 
        }

        public async Task<T> LoadAsync<T>(string fileName)
        {
            var bytes = await _storage.LoadAsync(fileName);
            if (bytes == null) return default;
            var currentVersion = PlayerPrefs.GetInt(GetVersionKey(fileName), _version);
            return await Task.Run(() => Unpack<T>(bytes, currentVersion));
        }

        public byte[] Pack<T>(T obj)
        {
            var raw = _serializer.Serialize(obj);
            raw = _compressor.Compress(raw);
            raw = _encryptor.Encrypt(raw);
            return _protector.Protect(raw);
        }

        private T Unpack<T>(byte[] data, int currentVersion)
        {
            var raw = _protector.Unprotect(data);
            raw = _encryptor.Decrypt(raw);
            raw = _compressor.Decompress(raw);
            var result = _serializer.Deserialize<T>(raw);
            
            if (currentVersion == _version) return result; 
            
            if (result is IMigratable migratable)
            {
                migratable.OnMigrate(currentVersion, _version);
            }

            StorageSystem.s_OnVersionChanged?.Invoke(result, currentVersion, _version);
            return result;
        }
    }
}