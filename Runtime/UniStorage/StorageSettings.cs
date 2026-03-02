using System;
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

    [Serializable]
    public class SerializableData
    {
        public int Version;

        public SerializationType SerializationType;
        public InterfaceReference<ISerializer> SerializerCustom;

        public KeyType KeyType;
        public InterfaceReference<IKey> KeyCustom;

        public EncryptionType EncryptionType;
        public InterfaceReference<IEncryptor> EncryptorCustom;

        public ProtectorType ProtectorType;
        public InterfaceReference<IProtector> ProtectorCustom;

        public StorageType StorageType;
        public InterfaceReference<IStorageProvider> StorageCustom;
    }

    public sealed class StorageSettings : ScriptableObject, ISettings
    {
        [SerializeField] internal SerializableData Data;

        public int Version => Data.Version;

        public ISerializer Serializer
        {
            get
            {
                return Data.SerializationType switch
                {
                    SerializationType.Binary => new BinarySerializer(),
                    SerializationType.Custom => Data.SerializerCustom.Value,
                    _ => new JsonSerializer()
                };
            }
        }

        public IKey Key
        {
            get
            {
                return Data.KeyType switch
                {
                    KeyType.DeviceBoundKey => new DeviceBoundKey(),
                    KeyType.Custom => Data.KeyCustom.Value,
                    _ => new StaticKey()
                };
            }
        }

        public IEncryptor Encryptor
        {
            get
            {
                return Data.EncryptionType switch
                {
                    EncryptionType.AES => new AESEncryptor(),
                    EncryptionType.Custom => Data.EncryptorCustom.Value,
                    _ => new NoEncryptor()
                };
            }
        }

        public IProtector Protector
        {
            get
            {
                return Data.ProtectorType switch
                {
                    ProtectorType.SHA256 => new SHA256Protector(),
                    ProtectorType.Custom => Data.ProtectorCustom.Value,
                    _ => new NoProtector()
                };
            }
        }

        public IStorageProvider StorageProvider
        {
            get
            {
                return Data.StorageType switch
                {
                    StorageType.PlayerPrefs => new PlayerPrefsStorage(),
                    StorageType.Custom => Data.StorageCustom.Value,
                    _ => new LocalStorage()
                };
            }
        }
    }

    public static class SettingsProvider
    {
        internal const string k_FileName = "UniCoreRuntimeStorageSettings";
        public static Func<StorageSettings> s_EditorInstanceProvider;
        public static StorageSettings Load()
        {
            var s_instance = Resources.Load<StorageSettings>(k_FileName);
            if (s_instance != null) return s_instance;
            
            if (s_EditorInstanceProvider != null)
            {
                s_instance = s_EditorInstanceProvider.Invoke();
                return s_instance;
            }
            
            s_instance = ScriptableObject.CreateInstance<StorageSettings>();
            return s_instance;
        }
    }
}