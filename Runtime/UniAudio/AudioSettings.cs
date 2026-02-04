#if HAS_UNITASK && HAS_ADDRESSABLES
using UniCore.Attribute;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniCore.Audio
{
    public sealed class AudioSettings : ScriptableObject
    {
        [SerializeField] private AddressGroupType addressType = AddressGroupType.Static;
        [SerializeField] private string staticAddress;
        [SerializeField] private InterfaceReference<IAddressGroup> customAddress;

        public string GroupAddress => addressType == AddressGroupType.Static ? staticAddress : customAddress.Value.GetAddress();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AudioSettings))]
    public class AudioSettingsEditor : Editor
    {
        private SerializedProperty addressTypeProperty;
        private SerializedProperty staticAddressProperty;
        private SerializedProperty customAddressProperty;

        private void OnEnable()
        {
            addressTypeProperty = serializedObject.FindProperty("addressType");
            staticAddressProperty = serializedObject.FindProperty("staticAddress");
            customAddressProperty = serializedObject.FindProperty("customAddress");
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
            EditorGUILayout.Space(5);
        }
    }
#endif
}
#endif