using System;
using UniCore.Attribute;
using UniCore.Utilities;

namespace UniCore.Storage
{
    public interface ISettings
    {
        public bool IsSystemEnabled { get; }
        public int Version { get; }
        public ISerializer Serializer { get; }
        public ICompressor Compressor { get; }
        public IKey Key { get; }
        public IEncryptor Encryptor { get; }
        public IProtector Protector { get; }
        public IStorageProvider StorageProvider { get; }
    }

    [Serializable]
    public struct SerializableData
    {
        public bool IsSystemEnabled;
        public int Version;

        public SerializationType SerializationType;
        public InterfaceReference<ISerializer> SerializerCustom;

        public CompressionType CompressionType;
        public InterfaceReference<ICompressor> CompressorCustom;

        public KeyType KeyType;
        public InterfaceReference<IKey> KeyCustom;

        public EncryptionType EncryptionType;
        public InterfaceReference<IEncryptor> EncryptorCustom;

        public ProtectorType ProtectorType;
        public InterfaceReference<IProtector> ProtectorCustom;

        public StorageType StorageType;
        public InterfaceReference<IStorageProvider> StorageCustom;
    }

    public sealed class StorageSettings : UniSettingsBase<StorageSettings, SerializableData, EditorStorageSettings>, ISettings
    {
        internal const string k_FileName = "UniCore_Runtime_StorageSettings";

        public bool IsSystemEnabled => Data.IsSystemEnabled;
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

        public ICompressor Compressor
        {
            get
            {
                return Data.CompressionType switch
                {
                    CompressionType.None => new NoCompressor(),
                    CompressionType.GZip => new GZipCompressor(),
                    _ => Data.CompressorCustom.Value
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
}