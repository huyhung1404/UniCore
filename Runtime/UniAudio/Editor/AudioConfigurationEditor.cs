using UnityEditor;
using UnityEngine;
using AudioConfiguration = UniCore.Audio.AudioConfiguration;

namespace UniCore.Editor.Audio
{
    [CustomEditor(typeof(AudioConfiguration))]
    public class AudioConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty _output, _mute, _bypassEffects, _bypassListenerEffects, _bypassReverbZones;
        private SerializedProperty _priority, _volume, _pitch, _stereoPan, _spatialBlend, _reverbZoneMix;
        private SerializedProperty _dopplerLevel, _spread, _volumeRolloff, _minDistance, _maxDistance;

        private bool _show3D = true;

        private void OnEnable()
        {
            _output = serializedObject.FindProperty("Output");
            _mute = serializedObject.FindProperty("Mute");
            _bypassEffects = serializedObject.FindProperty("BypassEffects");
            _bypassListenerEffects = serializedObject.FindProperty("BypassListenerEffects");
            _bypassReverbZones = serializedObject.FindProperty("BypassReverbZones");
            _priority = serializedObject.FindProperty("Priority");
            _volume = serializedObject.FindProperty("Volume");
            _pitch = serializedObject.FindProperty("Pitch");
            _stereoPan = serializedObject.FindProperty("StereoPan");
            _spatialBlend = serializedObject.FindProperty("SpatialBlend");
            _reverbZoneMix = serializedObject.FindProperty("ReverbZoneMix");
            _dopplerLevel = serializedObject.FindProperty("DopplerLevel");
            _spread = serializedObject.FindProperty("Spread");
            _volumeRolloff = serializedObject.FindProperty("VolumeRolloff");
            _minDistance = serializedObject.FindProperty("MinDistance");
            _maxDistance = serializedObject.FindProperty("MaxDistance");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeaderBlock();
            Space();
            DrawVolumeBlock();
            Space();
            DrawSpatialBlock();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeaderBlock()
        {
            EditorGUILayout.PropertyField(_output);
            EditorGUILayout.PropertyField(_mute);
            EditorGUILayout.PropertyField(_bypassEffects);
            EditorGUILayout.PropertyField(_bypassListenerEffects);
            EditorGUILayout.PropertyField(_bypassReverbZones);
        }

        private void DrawVolumeBlock()
        {
            DrawPrioritySlider();
            EditorGUILayout.Slider(_volume, 0, 1, "Volume");
            EditorGUILayout.Space(10);
            EditorGUILayout.Slider(_pitch, -3, 3, "Pitch");
            DrawSlider("Stereo Pan", _stereoPan, -1, 1, "Left", "Right");
            DrawSlider("Spatial Blend", _spatialBlend, 0, 1, "2D", "3D");
            EditorGUILayout.Slider(_reverbZoneMix, 0, 1.1f, "Reverb Zone Mix");
        }

        private void DrawPrioritySlider()
        {
            var rect = EditorGUILayout.GetControlRect();

            EditorGUI.IntSlider(rect, _priority, 0, 256, new GUIContent("Priority"));

            var labelRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 0.6f);
            labelRect.y -= 2;

            GUI.enabled = false;
            var mini = EditorStyles.miniLabel;
            EditorGUI.LabelField(new Rect(labelRect.x + EditorGUIUtility.labelWidth, labelRect.y - 4, 40, labelRect.height), "High", mini);
            EditorGUI.LabelField(new Rect(labelRect.xMax - 80, labelRect.y - 4, 40, labelRect.height), "Low", mini);
            GUI.enabled = true;
        }

        private void DrawSlider(string label, SerializedProperty property, float min, float max, string minLabel, string maxLabel)
        {
            var rect = EditorGUILayout.GetControlRect();

            EditorGUI.Slider(rect, property, min, max, new GUIContent(label));

            var labelRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 0.6f);
            labelRect.y -= 2;

            GUI.enabled = false;
            var mini = EditorStyles.miniLabel;
            EditorGUI.LabelField(new Rect(labelRect.x + EditorGUIUtility.labelWidth, labelRect.y - 4, 40, labelRect.height), minLabel, mini);
            EditorGUI.LabelField(new Rect(labelRect.xMax - 80, labelRect.y - 4, 40, labelRect.height), maxLabel, mini);
            GUI.enabled = true;
        }

        private void DrawSpatialBlock()
        {
            _show3D = EditorGUILayout.Foldout(_show3D, "3D Sound Settings", true);
            if (!_show3D) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.Slider(_dopplerLevel, 0, 5);
            EditorGUILayout.IntSlider(_spread, 0, 360);
            EditorGUILayout.PropertyField(_volumeRolloff);
            EditorGUILayout.PropertyField(_minDistance);
            EditorGUILayout.PropertyField(_maxDistance);

            EditorGUI.indentLevel--;
        }

        private static void Space() => EditorGUILayout.Space(10);
    }
}