using UniCore.Storage;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.Storage
{
    public static class StorageSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/UniCore/Storage Settings", SettingsScope.Project)
            {
                guiHandler = (_) =>
                {
                    var config = EditorStorageSettings.instance;
                    if (config == null) return;

                    var serializedObject = new SerializedObject(config);
                    serializedObject.Update();

                    var editorDataProp = serializedObject.FindProperty("EditorData");

                    EditorGUI.BeginChangeCheck();

                    OnGUI(editorDataProp);

                    if (!EditorGUI.EndChangeCheck()) return;
                    serializedObject.ApplyModifiedProperties();
                    config.SaveData();
                }
            };
            return provider;
        }

        private static void OnGUI(SerializedProperty dataProperty)
        {
            var versionProp = dataProperty.FindPropertyRelative("Version");

            var serializationTypeProp = dataProperty.FindPropertyRelative("SerializationType");
            var serializerCustomProp = dataProperty.FindPropertyRelative("SerializerCustom");

            var keyTypeProp = dataProperty.FindPropertyRelative("KeyType");
            var keyCustomProp = dataProperty.FindPropertyRelative("KeyCustom");

            var encryptionTypeProp = dataProperty.FindPropertyRelative("EncryptionType");
            var encryptorCustomProp = dataProperty.FindPropertyRelative("EncryptorCustom");

            var protectorTypeProp = dataProperty.FindPropertyRelative("ProtectorType");
            var protectorCustomProp = dataProperty.FindPropertyRelative("ProtectorCustom");

            var storageTypeProp = dataProperty.FindPropertyRelative("StorageType");
            var storageCustomProp = dataProperty.FindPropertyRelative("StorageCustom");

            EditorGUILayout.PropertyField(versionProp);
            EditorGUILayout.Space(5);

            DrawField("Serialize", serializationTypeProp, (int)SerializationType.Custom, serializerCustomProp);

            if (encryptionTypeProp.enumValueIndex == (int)EncryptionType.AES ||
                protectorTypeProp.enumValueIndex == (int)ProtectorType.SHA256)
            {
                DrawField("Key", keyTypeProp, (int)KeyType.Custom, keyCustomProp);
            }

            DrawField("Encryptor", encryptionTypeProp, (int)EncryptionType.Custom, encryptorCustomProp);
            DrawField("Protector", protectorTypeProp, (int)ProtectorType.Custom, protectorCustomProp);
            DrawField("Storage", storageTypeProp, (int)StorageType.Custom, storageCustomProp);
        }

        private static void DrawField(string title, SerializedProperty enumProperty, int targetCustomId, SerializedProperty customProperty)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(enumProperty, GUIContent.none);

            if (customProperty != null && enumProperty.enumValueIndex == targetCustomId)
            {
                EditorGUILayout.PropertyField(customProperty, GUIContent.none);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }
    }
}