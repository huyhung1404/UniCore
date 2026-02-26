using UniCore.Storage;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor
{
    [CustomEditor(typeof(StorageSettings))]
    public class StorageSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _versionProperty;

        private SerializedProperty _serializationTypeProperty;
        private SerializedProperty _serializerCustomProperty;

        private SerializedProperty _keyTypeProperty;
        private SerializedProperty _keyCustomProperty;

        private SerializedProperty _encryptionTypeProperty;
        private SerializedProperty _encryptorCustomProperty;

        private SerializedProperty _protectorTypeProperty;
        private SerializedProperty _protectorCustomProperty;

        private SerializedProperty _storageTypeProperty;
        private SerializedProperty _storageCustomProperty;

        [MenuItem("UniCore/Settings/Storage", priority = 1)]
        public static void CreateStorageSettings()
        {
            var assetPath = $"Assets/Resources/{nameof(StorageSettings)}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<StorageSettings>(assetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var settings = CreateInstance<StorageSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        private void OnEnable()
        {
            _versionProperty = serializedObject.FindProperty("m_version");

            _serializationTypeProperty = serializedObject.FindProperty("m_serializationType");
            _serializerCustomProperty = serializedObject.FindProperty("m_serializerCustom");

            _keyTypeProperty = serializedObject.FindProperty("m_keyType");
            _keyCustomProperty = serializedObject.FindProperty("m_keyCustom");

            _encryptionTypeProperty = serializedObject.FindProperty("m_encryptionType");
            _encryptorCustomProperty = serializedObject.FindProperty("m_encryptorCustom");

            _protectorTypeProperty = serializedObject.FindProperty("m_protectorType");
            _protectorCustomProperty = serializedObject.FindProperty("m_protectorCustom");

            _storageTypeProperty = serializedObject.FindProperty("m_storageType");
            _storageCustomProperty = serializedObject.FindProperty("m_storageCustom");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(_versionProperty);
            EditorGUILayout.Space(5);

            DrawField("Serialize", _serializationTypeProperty, (int)SerializationType.Custom, _serializerCustomProperty);

            if (_encryptionTypeProperty.enumValueIndex == (int)EncryptionType.AES ||
                _protectorTypeProperty.enumValueIndex == (int)ProtectorType.SHA256)
            {
                DrawField("Key", _keyTypeProperty, (int)KeyType.Custom, _keyCustomProperty);
            }

            DrawField("Encryptor", _encryptionTypeProperty, (int)EncryptionType.Custom, _encryptorCustomProperty);
            DrawField("Protector", _protectorTypeProperty, (int)ProtectorType.Custom, _protectorCustomProperty);
            DrawField("Storage", _storageTypeProperty, (int)StorageType.Custom, _storageCustomProperty);
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