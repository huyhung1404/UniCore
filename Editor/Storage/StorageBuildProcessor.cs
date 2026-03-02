using UniCore.Storage;

namespace UniCore.Editor.Storage
{
    public sealed class StorageBuildProcessor : BuildProcessor<StorageSettings, SerializableData, EditorStorageSettings>
    {
        public override string FileName() => StorageSettings.k_FileName;
    }
}