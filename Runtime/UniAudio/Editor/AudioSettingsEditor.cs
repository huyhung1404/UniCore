#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;
using UniCore.Audio;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using AudioSettings = UniCore.Audio.AudioSettings;

namespace UniCore.Editor.Audio
{
    [CustomEditor(typeof(AudioSettings))]
    public class AudioSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty _addressTypeProperty;
        private SerializedProperty _staticAddressProperty;
        private SerializedProperty _customAddressProperty;
        private SerializedProperty _soundEmitterPrefabProperty;
        private SerializedProperty _poolInitialSizeProperty;
        private SerializedProperty _outputMixerProperty;
        private string _validationError;
        private string _rootFolderPath;
        private string _nodesFolderPath;
        private string _configsFolderPath;

        private void OnEnable()
        {
            _addressTypeProperty = serializedObject.FindProperty("m_addressType");
            _staticAddressProperty = serializedObject.FindProperty("m_staticAddress");
            _customAddressProperty = serializedObject.FindProperty("m_customAddress");
            _soundEmitterPrefabProperty = serializedObject.FindProperty("m_soundEmitterPrefab");
            _poolInitialSizeProperty = serializedObject.FindProperty("m_poolInitialSize");
            _outputMixerProperty = serializedObject.FindProperty("m_outputMixer");
        }

        [MenuItem("UniCore/Settings/Audio", priority = 1)]
        public static void CreateSettings()
        {
            var assetPath = $"Assets/Resources/{nameof(AudioSettings)}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AudioSettings>(assetPath);
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

            var settings = CreateInstance<AudioSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public override void OnInspectorGUI()
        {
            DrawSettings();
            DrawAddress();
            ValidateAddress();
            DrawFolderStructureGUI();

            if (string.IsNullOrEmpty(_validationError))
            {
                if (!GUILayout.Button("Ping")) return;
                var obj = AssetDatabase.LoadAssetAtPath<Object>(_rootFolderPath);
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
                return;
            }

            EditorGUILayout.HelpBox(_validationError, MessageType.Error);

            if (!GUILayout.Button("Fix")) return;
            var settings = (AudioSettings)target;
            var address = settings.GroupAddress;
            FixStructure(address);
            ValidateAddress();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Configs", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_outputMixerProperty);
            EditorGUILayout.PropertyField(_soundEmitterPrefabProperty);
            EditorGUILayout.PropertyField(_poolInitialSizeProperty);
            EditorGUI.indentLevel--;
        }

        private void DrawAddress()
        {
            EditorGUILayout.LabelField("Group Address", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_addressTypeProperty, GUIContent.none);
            if (_addressTypeProperty != null && _addressTypeProperty.enumValueIndex == (int)AddressGroupType.Custom)
            {
                EditorGUILayout.PropertyField(_customAddressProperty, GUIContent.none);
            }
            else
            {
                EditorGUILayout.PropertyField(_staticAddressProperty, GUIContent.none);
            }

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateAddress()
        {
            _validationError = null;
            _rootFolderPath = null;
            _nodesFolderPath = null;
            _configsFolderPath = null;

            var settings = (AudioSettings)target;
            var address = settings.GroupAddress;

            if (string.IsNullOrEmpty(address))
            {
                _validationError = "GroupAddress is empty.";
                return;
            }

            _rootFolderPath = address;

            if (!AssetDatabase.IsValidFolder(_rootFolderPath))
            {
                _validationError = $"Folder not found at path: {_rootFolderPath}";
                return;
            }

            _nodesFolderPath = $"{_rootFolderPath}/Nodes";
            _configsFolderPath = $"{_rootFolderPath}/Configs";

            var missing = "";
            if (!AssetDatabase.IsValidFolder(_nodesFolderPath))
                missing += "Nodes folder missing. ";

            if (!AssetDatabase.IsValidFolder(_configsFolderPath))
                missing += "Configs folder missing.";

            if (!string.IsNullOrEmpty(missing))
                _validationError = missing;
        }

        private void FixStructure(string address)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (aaSettings == null) return;

            _rootFolderPath = address;

            if (!AssetDatabase.IsValidFolder(_rootFolderPath))
            {
                var parent = System.IO.Path.GetDirectoryName(_rootFolderPath);
                var folderName = System.IO.Path.GetFileName(_rootFolderPath);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            _nodesFolderPath = $"{_rootFolderPath}/Nodes";
            _configsFolderPath = $"{_rootFolderPath}/Configs";

            if (!AssetDatabase.IsValidFolder(_nodesFolderPath))
                AssetDatabase.CreateFolder(_rootFolderPath, "Nodes");

            if (!AssetDatabase.IsValidFolder(_configsFolderPath))
                AssetDatabase.CreateFolder(_rootFolderPath, "Configs");

            var guid = AssetDatabase.AssetPathToGUID(_rootFolderPath);
            var entry = aaSettings.FindAssetEntry(guid);

            if (entry == null)
            {
                var group = GetOrCreateAudioGroup(aaSettings);
                entry = aaSettings.CreateOrMoveEntry(guid, group);
                entry.address = address;
            }

            EditorUtility.SetDirty(aaSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static AddressableAssetGroup GetOrCreateAudioGroup(AddressableAssetSettings aaSettings)
        {
            var group = aaSettings.FindGroup("AudioGroup");
            if (group != null) return group;

            var schemas = new List<AddressableAssetGroupSchema>();
            foreach (var schema in aaSettings.DefaultGroup.Schemas)
            {
                var type = schema.GetType();
                var instance = CreateInstance(type) as AddressableAssetGroupSchema;
                if (instance == null) continue;
                instance.name = type.Name;
                schemas.Add(instance);
            }

            group = aaSettings.CreateGroup("AudioGroup", false, false, false, schemas);
            return group;
        }

        private void DrawFolderStructureGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Required Folder Structure", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (string.IsNullOrEmpty(_rootFolderPath) || !AssetDatabase.IsValidFolder(_rootFolderPath))
            {
                DrawDefault(_rootFolderPath);

                EditorGUI.indentLevel++;
                DrawDefault("Nodes");
                DrawDefault("Configs");
                EditorGUI.indentLevel -= 2;

                return;
            }

            DrawFolderLine(_rootFolderPath, true);

            EditorGUI.indentLevel++;
            DrawFolderLine(System.IO.Path.GetFileName(_nodesFolderPath), AssetDatabase.IsValidFolder(_nodesFolderPath));
            DrawFolderLine(System.IO.Path.GetFileName(_configsFolderPath), AssetDatabase.IsValidFolder(_configsFolderPath));
            EditorGUI.indentLevel -= 2;
        }

        private static void DrawDefault(string fileName)
        {
            var color = Color.red;
            var prev = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"✖ {fileName}");
            GUI.color = prev;
        }

        private static void DrawFolderLine(string fileName, bool exists)
        {
            var icon = exists ? "✔" : "✖";
            var color = exists ? Color.green : Color.red;
            var prev = GUI.color;
            GUI.color = color;
            EditorGUILayout.LabelField($"{icon} {fileName}");
            GUI.color = prev;
        }
    }
}
#endif