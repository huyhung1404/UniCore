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
        private SerializedProperty addressTypeProperty;
        private SerializedProperty staticAddressProperty;
        private SerializedProperty customAddressProperty;
        private SerializedProperty soundEmitterPrefabProperty;
        private SerializedProperty poolInitialSizeProperty;
        private SerializedProperty outputMixerProperty;
        private string validationError;
        private string rootFolderPath;
        private string nodesFolderPath;
        private string configsFolderPath;

        private void OnEnable()
        {
            addressTypeProperty = serializedObject.FindProperty("addressType");
            staticAddressProperty = serializedObject.FindProperty("staticAddress");
            customAddressProperty = serializedObject.FindProperty("customAddress");
            soundEmitterPrefabProperty = serializedObject.FindProperty("soundEmitterPrefab");
            poolInitialSizeProperty = serializedObject.FindProperty("poolInitialSize");
            outputMixerProperty = serializedObject.FindProperty("outputMixer");
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

            if (string.IsNullOrEmpty(validationError))
            {
                if (!GUILayout.Button("Ping")) return;
                var obj = AssetDatabase.LoadAssetAtPath<Object>(rootFolderPath);
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
                return;
            }

            EditorGUILayout.HelpBox(validationError, MessageType.Error);

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
            EditorGUILayout.PropertyField(outputMixerProperty);
            EditorGUILayout.PropertyField(soundEmitterPrefabProperty);
            EditorGUILayout.PropertyField(poolInitialSizeProperty);
            EditorGUI.indentLevel--;
        }

        private void DrawAddress()
        {
            EditorGUILayout.LabelField("Group Address", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(addressTypeProperty, GUIContent.none);
            if (addressTypeProperty != null && addressTypeProperty.enumValueIndex == (int)AddressGroupType.Custom)
            {
                EditorGUILayout.PropertyField(customAddressProperty, GUIContent.none);
            }
            else
            {
                EditorGUILayout.PropertyField(staticAddressProperty, GUIContent.none);
            }

            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }

        private void ValidateAddress()
        {
            validationError = null;
            rootFolderPath = null;
            nodesFolderPath = null;
            configsFolderPath = null;

            var settings = (AudioSettings)target;
            var address = settings.GroupAddress;

            if (string.IsNullOrEmpty(address))
            {
                validationError = "GroupAddress is empty.";
                return;
            }

            rootFolderPath = address;

            if (!AssetDatabase.IsValidFolder(rootFolderPath))
            {
                validationError = $"Folder not found at path: {rootFolderPath}";
                return;
            }

            nodesFolderPath = $"{rootFolderPath}/Nodes";
            configsFolderPath = $"{rootFolderPath}/Configs";

            var missing = "";
            if (!AssetDatabase.IsValidFolder(nodesFolderPath))
                missing += "Nodes folder missing. ";

            if (!AssetDatabase.IsValidFolder(configsFolderPath))
                missing += "Configs folder missing.";

            if (!string.IsNullOrEmpty(missing))
                validationError = missing;
        }

        private void FixStructure(string address)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (aaSettings == null) return;

            rootFolderPath = address;

            if (!AssetDatabase.IsValidFolder(rootFolderPath))
            {
                var parent = System.IO.Path.GetDirectoryName(rootFolderPath);
                var folderName = System.IO.Path.GetFileName(rootFolderPath);
                AssetDatabase.CreateFolder(parent, folderName);
            }

            nodesFolderPath = $"{rootFolderPath}/Nodes";
            configsFolderPath = $"{rootFolderPath}/Configs";

            if (!AssetDatabase.IsValidFolder(nodesFolderPath))
                AssetDatabase.CreateFolder(rootFolderPath, "Nodes");

            if (!AssetDatabase.IsValidFolder(configsFolderPath))
                AssetDatabase.CreateFolder(rootFolderPath, "Configs");

            var guid = AssetDatabase.AssetPathToGUID(rootFolderPath);
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

            if (string.IsNullOrEmpty(rootFolderPath) || !AssetDatabase.IsValidFolder(rootFolderPath))
            {
                DrawDefault(rootFolderPath);

                EditorGUI.indentLevel++;
                DrawDefault("Nodes");
                DrawDefault("Configs");
                EditorGUI.indentLevel -= 2;

                return;
            }

            DrawFolderLine(rootFolderPath, true);

            EditorGUI.indentLevel++;
            DrawFolderLine(System.IO.Path.GetFileName(nodesFolderPath), AssetDatabase.IsValidFolder(nodesFolderPath));
            DrawFolderLine(System.IO.Path.GetFileName(configsFolderPath), AssetDatabase.IsValidFolder(configsFolderPath));
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