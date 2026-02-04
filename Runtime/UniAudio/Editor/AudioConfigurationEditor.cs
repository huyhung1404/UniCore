using UnityEditor;
using UnityEngine;
using AudioConfiguration = UniCore.Audio.AudioConfiguration;

namespace UniCore.Editor.Audio
{
    [CustomEditor(typeof(AudioConfiguration))]
    public class AudioConfigurationEditor : UnityEditor.Editor
    {
        private SerializedProperty output, mute, bypassEffects, bypassListenerEffects, bypassReverbZones;
        private SerializedProperty priority, volume, pitch, stereoPan, spatialBlend, reverbZoneMix;
        private SerializedProperty dopplerLevel, spread, volumeRolloff, minDistance, maxDistance;

        private bool show3D = true;

        private void OnEnable()
        {
            output = serializedObject.FindProperty("output");
            mute = serializedObject.FindProperty("mute");
            bypassEffects = serializedObject.FindProperty("bypassEffects");
            bypassListenerEffects = serializedObject.FindProperty("bypassListenerEffects");
            bypassReverbZones = serializedObject.FindProperty("bypassReverbZones");
            priority = serializedObject.FindProperty("priority");
            volume = serializedObject.FindProperty("volume");
            pitch = serializedObject.FindProperty("pitch");
            stereoPan = serializedObject.FindProperty("stereoPan");
            spatialBlend = serializedObject.FindProperty("spatialBlend");
            reverbZoneMix = serializedObject.FindProperty("reverbZoneMix");
            dopplerLevel = serializedObject.FindProperty("dopplerLevel");
            spread = serializedObject.FindProperty("spread");
            volumeRolloff = serializedObject.FindProperty("volumeRolloff");
            minDistance = serializedObject.FindProperty("minDistance");
            maxDistance = serializedObject.FindProperty("maxDistance");
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
            EditorGUILayout.PropertyField(output);
            EditorGUILayout.PropertyField(mute);
            EditorGUILayout.PropertyField(bypassEffects);
            EditorGUILayout.PropertyField(bypassListenerEffects);
            EditorGUILayout.PropertyField(bypassReverbZones);
        }

        private void DrawVolumeBlock()
        {
            DrawPrioritySlider();
            EditorGUILayout.Slider(volume, 0, 1, "Volume");
            EditorGUILayout.Space(10);
            EditorGUILayout.Slider(pitch, -3, 3, "Pitch");
            DrawSlider("Stereo Pan", stereoPan, -1, 1, "Left", "Right");
            DrawSlider("Spatial Blend", spatialBlend, 0, 1, "2D", "3D");
            EditorGUILayout.Slider(reverbZoneMix, 0, 1.1f, "Reverb Zone Mix");
        }

        private void DrawPrioritySlider()
        {
            var rect = EditorGUILayout.GetControlRect();

            EditorGUI.IntSlider(rect, priority, 0, 256, new GUIContent("Priority"));

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
            show3D = EditorGUILayout.Foldout(show3D, "3D Sound Settings", true);
            if (!show3D) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.Slider(dopplerLevel, 0, 5);
            EditorGUILayout.IntSlider(spread, 0, 360);
            EditorGUILayout.PropertyField(volumeRolloff);
            EditorGUILayout.PropertyField(minDistance);
            EditorGUILayout.PropertyField(maxDistance);

            EditorGUI.indentLevel--;
        }

        private static void Space() => EditorGUILayout.Space(10);
    }
}