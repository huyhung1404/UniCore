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
        [SerializeField] private int m_version;

        [SerializeField] private SerializationType m_serializationType = SerializationType.Json;
        [SerializeField] private InterfaceReference<ISerializer> m_serializerCustom;

        [SerializeField] private KeyType m_keyType = KeyType.Static;
        [SerializeField] private InterfaceReference<IKey> m_keyCustom;

        [SerializeField] private EncryptionType m_encryptionType = EncryptionType.None;
        [SerializeField] private InterfaceReference<IEncryptor> m_encryptorCustom;

        [SerializeField] private ProtectorType m_protectorType = ProtectorType.None;
        [SerializeField] private InterfaceReference<IProtector> m_protectorCustom;

        [SerializeField] private StorageType m_storageType = StorageType.LocalStorage;
        [SerializeField] private InterfaceReference<IStorageProvider> m_storageCustom;

        public int Version => m_version;

        public ISerializer Serializer
        {
            get
            {
                return m_serializationType switch
                {
                    SerializationType.Binary => new BinarySerializer(),
                    SerializationType.Custom => m_serializerCustom.Value,
                    _ => new JsonSerializer()
                };
            }
        }

        public IKey Key
        {
            get
            {
                return m_keyType switch
                {
                    KeyType.DeviceBoundKey => new DeviceBoundKey(),
                    KeyType.Custom => m_keyCustom.Value,
                    _ => new StaticKey()
                };
            }
        }

        public IEncryptor Encryptor
        {
            get
            {
                return m_encryptionType switch
                {
                    EncryptionType.AES => new AESEncryptor(),
                    EncryptionType.Custom => m_encryptorCustom.Value,
                    _ => new NoEncryptor()
                };
            }
        }

        public IProtector Protector
        {
            get
            {
                return m_protectorType switch
                {
                    ProtectorType.SHA256 => new SHA256Protector(),
                    ProtectorType.Custom => m_protectorCustom.Value,
                    _ => new NoProtector()
                };
            }
        }

        public IStorageProvider StorageProvider
        {
            get
            {
                return m_storageType switch
                {
                    StorageType.PlayerPrefs => new PlayerPrefsStorage(),
                    StorageType.Custom => m_storageCustom.Value,
                    _ => new LocalStorage()
                };
            }
        }
    }
}