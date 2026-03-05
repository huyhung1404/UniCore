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
            var provider = new SettingsProvider("Project/UniCore/Storage", SettingsScope.Project)
            {
                keywords = new[] { "Storage", "Save", "Encrypt", "Compress", "UniCore", "Security" },
                guiHandler = (_) =>
                {
                    var config = EditorStorageSettings.instance;
                    if (config == null) return;

                    var serializedObject = new SerializedObject(config);
                    serializedObject.Update();

                    var editorDataProp = serializedObject.FindProperty("EditorData");

                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(5, 5, 5, 5) });

                    EditorGUI.BeginChangeCheck();

                    OnGUI(editorDataProp);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        config.SaveData();
                    }

                    EditorGUILayout.EndVertical();
                }
            };
            return provider;
        }

        private static void OnGUI(SerializedProperty dataProperty)
        {
            var enableProp = dataProperty.FindPropertyRelative("IsSystemEnabled");
            var isSystemEnabled = UniEditorGUI.DrawMasterToggle(enableProp);
            if (!isSystemEnabled) return;
            
            var versionProp = dataProperty.FindPropertyRelative("Version");

            var serializationTypeProp = dataProperty.FindPropertyRelative("SerializationType");
            var serializerCustomProp = dataProperty.FindPropertyRelative("SerializerCustom");

            var compressionTypeProp = dataProperty.FindPropertyRelative("CompressionType");
            var compressorCustomProp = dataProperty.FindPropertyRelative("CompressorCustom");

            var keyTypeProp = dataProperty.FindPropertyRelative("KeyType");
            var keyCustomProp = dataProperty.FindPropertyRelative("KeyCustom");

            var encryptionTypeProp = dataProperty.FindPropertyRelative("EncryptionType");
            var encryptorCustomProp = dataProperty.FindPropertyRelative("EncryptorCustom");

            var protectorTypeProp = dataProperty.FindPropertyRelative("ProtectorType");
            var protectorCustomProp = dataProperty.FindPropertyRelative("ProtectorCustom");

            var storageTypeProp = dataProperty.FindPropertyRelative("StorageType");
            var storageCustomProp = dataProperty.FindPropertyRelative("StorageCustom");

            EditorGUILayout.LabelField("Core Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.Space(3);
            EditorGUILayout.PropertyField(versionProp);
            EditorGUILayout.Space(3);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Data Handling", EditorStyles.boldLabel);
            DrawField("Serialization Type", "d_TextAsset Icon", serializationTypeProp, (int)SerializationType.Custom, serializerCustomProp);
            DrawField("Compression Layer", "d_ScaleTool", compressionTypeProp, (int)UniCore.Storage.CompressionType.Custom, compressorCustomProp);
            DrawField("Storage Medium", "d_Folder Icon", storageTypeProp, (int)StorageType.Custom, storageCustomProp);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Security Layer", EditorStyles.boldLabel);
            DrawField("Encryptor Type", "LockIcon", encryptionTypeProp, (int)EncryptionType.Custom, encryptorCustomProp);
            DrawField("Protector Hash", "d_FilterByType", protectorTypeProp, (int)ProtectorType.Custom, protectorCustomProp);

            if (encryptionTypeProp.enumValueIndex == (int)EncryptionType.AES ||
                protectorTypeProp.enumValueIndex == (int)ProtectorType.SHA256)
            {
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.8f, 0.8f, 1f);
                DrawField("Encryption Key", "d_FilterByLabel", keyTypeProp, (int)KeyType.Custom, keyCustomProp);
                GUI.backgroundColor = prevColor;
            }
        }

        private static void DrawField(string title, string iconId, SerializedProperty enumProperty, int targetCustomId, SerializedProperty customProperty)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.Space(2);

            var headerContent = GetSafeHeaderContent(title, iconId);
            EditorGUILayout.LabelField(headerContent, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enumProperty, GUIContent.none);

            if (customProperty != null && enumProperty.enumValueIndex == targetCustomId)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(customProperty, GUIContent.none);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private static GUIContent GetSafeHeaderContent(string title, string iconId)
        {
            var iconContent = EditorGUIUtility.IconContent(iconId);

            if (iconContent != null && iconContent.image != null)
            {
                return new GUIContent($" {title}", iconContent.image);
            }

            var fallbackIcon = EditorGUIUtility.IconContent("d_GameObject Icon").image;
            return new GUIContent($" {title}", fallbackIcon);
        }
    }
}