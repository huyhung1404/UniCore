using UniCore.Attribute;
using UnityEngine;

namespace UniCore.Storage
{
    public interface ISettings
    {
        public int Version { get; }
        public ISerializer Serializer { get; }
        public IKey Key { get; }
        public IEncryptor Encryptor { get; }
        public IProtector Protector { get; }
        public IStorageProvider StorageProvider { get; }
    }

    public sealed class StorageSettings : ScriptableObject, ISettings
    {
        [SerializeField] private int version;

        [SerializeField] private SerializationType serializationType = SerializationType.Json;
        [SerializeField] private InterfaceReference<ISerializer> serializerCustom;

        [SerializeField] private KeyType keyType = KeyType.Static;
        [SerializeField] private InterfaceReference<IKey> keyCustom;

        [SerializeField] private EncryptionType encryptionType = EncryptionType.None;
        [SerializeField] private InterfaceReference<IEncryptor> encryptorCustom;

        [SerializeField] private ProtectorType protectorType = ProtectorType.None;
        [SerializeField] private InterfaceReference<IProtector> protectorCustom;

        [SerializeField] private StorageType storageType = StorageType.LocalStorage;
        [SerializeField] private InterfaceReference<IStorageProvider> storageCustom;

        public int Version => version;

        public ISerializer Serializer
        {
            get
            {
                return serializationType switch
                {
                    SerializationType.Binary => new BinarySerializer(),
                    SerializationType.Custom => serializerCustom.Value,
                    _ => new JsonSerializer()
                };
            }
        }

        public IKey Key
        {
            get
            {
                return keyType switch
                {
                    KeyType.DeviceBoundKey => new DeviceBoundKey(),
                    KeyType.Custom => keyCustom.Value,
                    _ => new StaticKey()
                };
            }
        }

        public IEncryptor Encryptor
        {
            get
            {
                return encryptionType switch
                {
                    EncryptionType.AES => new AESEncryptor(),
                    EncryptionType.Custom => encryptorCustom.Value,
                    _ => new NoEncryptor()
                };
            }
        }

        public IProtector Protector
        {
            get
            {
                return protectorType switch
                {
                    ProtectorType.SHA256 => new SHA256Protector(),
                    ProtectorType.Custom => protectorCustom.Value,
                    _ => new NoProtector()
                };
            }
        }

        public IStorageProvider StorageProvider
        {
            get
            {
                return storageType switch
                {
                    StorageType.PlayerPrefs => new PlayerPrefsStorage(),
                    StorageType.Custom => storageCustom.Value,
                    _ => new LocalStorage()
                };
            }
        }
    }
}