using UniCore.Utilities;

namespace UniCore.Storage
{
#if UNITY_EDITOR
    [UnityEditor.FilePath("ProjectSettings/UniCore_StorageSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
#endif
    public sealed class EditorStorageSettings : UniSettingsEditorBase<EditorStorageSettings, SerializableData>
    {
    }
}