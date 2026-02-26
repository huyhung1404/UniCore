using UnityEngine;

namespace UniCore.Storage
{
    public class StoragePipeline
    {
        public byte[] Key { get; private set; }
        private ISerializer _serializer;
        private IEncryptor _encryptor;
        private IProtector _protector;
        private IStorageProvider _storage;
        private int _version;

        public StoragePipeline(ISettings settings)
        {
            if (settings == null)
            {
                LoadSettingDefault();
                return;
            }

            Key = settings.Key.GetKey();
            _serializer = settings.Serializer;
            _encryptor = settings.Encryptor;
            _protector = settings.Protector;
            _storage = settings.StorageProvider;
            _version = settings.Version;
        }

        private void LoadSettingDefault()
        {
            Key = null;
            _serializer = new JsonSerializer();
            _encryptor = new NoEncryptor();
            _protector = new NoProtector();
            _storage = new LocalStorage();
        }

        public void Save<T>(string fileName, T data)
        {
            var bytes = Pack(data);
            _storage.Save(fileName, bytes);
            PlayerPrefs.SetInt("storage_version", _version);
            PlayerPrefs.Save();
        }

        public T Load<T>(string fileName)
        {
            var bytes = _storage.Load(fileName);
            return bytes == null ? default : Unpack<T>(bytes);
        }

        public byte[] Pack<T>(T obj)
        {
            var raw = _serializer.Serialize(obj);
            raw = _encryptor.Encrypt(raw);
            return _protector.Protect(raw);
        }

        public T Unpack<T>(byte[] data)
        {
            var raw = _protector.Unprotect(data);
            raw = _encryptor.Decrypt(raw);
            var result = _serializer.Deserialize<T>(raw);
            var v = PlayerPrefs.GetInt("storage_version", _version);
            if (v != _version) StorageSystem.s_OnVersionChanged?.Invoke(result, v, _version);
            return result;
        }
    }
}