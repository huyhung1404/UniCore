using UniCore.Storage;
using UnityEditor;
using UnityEngine;
using SettingsProvider = UniCore.Storage.SettingsProvider;

namespace UniCore.Editor.Storage
{
    [FilePath("ProjectSettings/UniCoreStorageSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class StorageEditorConfig : ScriptableSingleton<StorageEditorConfig>
    {
        public SerializableData EditorData;

        internal void SaveData()
        {
            Save(true);
        }
    }

    [InitializeOnLoad]
    public static class StoragePlayModeInjector
    {
        static StoragePlayModeInjector()
        {
            SettingsProvider.s_EditorInstanceProvider = CreateInMemorySettings;
        }

        private static StorageSettings CreateInMemorySettings()
        {
            var editorConfig = StorageEditorConfig.instance;
            if (editorConfig == null) return ScriptableObject.CreateInstance<StorageSettings>();

            var runtimeSettings = ScriptableObject.CreateInstance<StorageSettings>();
            runtimeSettings.Data = editorConfig.EditorData;

            return runtimeSettings;
        }
    }
}