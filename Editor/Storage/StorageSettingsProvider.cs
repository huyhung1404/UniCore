using UniCore.Storage;
using UnityEditor;

namespace UniCore.Editor.Storage
{
    public static class StorageSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/UniCore/Storage Settings", SettingsScope.Project)
            {
                guiHandler = (searchContext) =>
                {
                    var config = EditorStorageSettings.instance;
                    
                    if (config == null) return; // Early return

                    EditorGUI.BeginChangeCheck();

                    config.EditorData.Version = EditorGUILayout.IntField("Version", config.EditorData.Version);
                    // config.SerializationType = (SerializationType)EditorGUILayout.EnumPopup("Serialization", config.SerializationType);
                    // ... (Draw UI cho các trường khác)

                    if (EditorGUI.EndChangeCheck())
                    {
                        config.SaveData();
                    }
                }
            };
            return provider;
        }
    }
}