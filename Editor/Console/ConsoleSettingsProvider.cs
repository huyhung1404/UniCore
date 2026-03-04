using UniCore.Console;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.Console
{
    public static class ConsoleSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/UniCore/Console", SettingsScope.Project)
            {
                keywords = new[] { "Console", "UniCore", "Debug", "Developer", "Cheat", "Optimization" },
                guiHandler = (_) =>
                {
                    var config = ConsoleEditorSettings.instance;
                    if (config == null) return;

                    var serializedObject = new SerializedObject(config);
                    serializedObject.Update();

                    var editorDataProp = serializedObject.FindProperty("EditorData");

                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(15, 15, 15, 15) });

                    EditorGUI.BeginChangeCheck();

                    DrawSettingsGUI(editorDataProp);

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

        private static void DrawSettingsGUI(SerializedProperty dataProperty)
        {
            var passwordProp = dataProperty.FindPropertyRelative("m_password");

            var loginTriggerProp = dataProperty.FindPropertyRelative("m_loginTriggerMode");
            var loginTapCountProp = dataProperty.FindPropertyRelative("m_loginTapCount");
            var loginTapTimeoutProp = dataProperty.FindPropertyRelative("m_loginTapTimeout");
            var loginLongPressProp = dataProperty.FindPropertyRelative("m_loginLongPressDuration");

            var openTriggerProp = dataProperty.FindPropertyRelative("m_openTriggerMode");
            var openTapCountProp = dataProperty.FindPropertyRelative("m_openTapCount");
            var openTapTimeoutProp = dataProperty.FindPropertyRelative("m_openTapTimeout");
            var openLongPressProp = dataProperty.FindPropertyRelative("m_openLongPressDuration");

            // Lấy property phím tắt mới
            var openConsoleKeyProp = dataProperty.FindPropertyRelative("m_openConsoleKey");

            var guiScaleProp = dataProperty.FindPropertyRelative("m_guiScaleMultiplier");
            var refMinDimProp = dataProperty.FindPropertyRelative("m_referenceMinDimension");
            var guiOpacityProp = dataProperty.FindPropertyRelative("m_guiOpacity");

            var maxLogsProp = dataProperty.FindPropertyRelative("m_maxLogs");
            var captureLogProp = dataProperty.FindPropertyRelative("m_captureLogStackTrace");
            var captureWarnProp = dataProperty.FindPropertyRelative("m_captureWarningStackTrace");
            var captureErrorProp = dataProperty.FindPropertyRelative("m_captureErrorStackTrace");

            void DrawTriggerParams(SerializedProperty modeProp, SerializedProperty tapCount, SerializedProperty tapTime, SerializedProperty longPress)
            {
                EditorGUI.indentLevel++;
                if (modeProp.enumValueIndex == (int)TriggerMode.MultiTaps)
                {
                    EditorGUILayout.PropertyField(tapCount, new GUIContent("Tap Count"));
                    EditorGUILayout.PropertyField(tapTime, new GUIContent("Tap Timeout (s)"));
                }
                else if (modeProp.enumValueIndex == (int)TriggerMode.LongPress)
                {
                    EditorGUILayout.PropertyField(longPress, new GUIContent("Long Press Time (s)"));
                }
                else if (modeProp.enumValueIndex == (int)TriggerMode.DrawCircle)
                {
                    EditorGUILayout.HelpBox("Require scanning the screen in a circular sweep (\u2265 320 degrees).", MessageType.None);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(passwordProp, new GUIContent("Password"));
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Trigger Behaviors", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Login Trigger (Guest -> Dev)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(loginTriggerProp, new GUIContent("Mode"));
            DrawTriggerParams(loginTriggerProp, loginTapCountProp, loginTapTimeoutProp, loginLongPressProp);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Open Console Trigger (Dev Only)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(openTriggerProp, new GUIContent("Touch Mode"));
            DrawTriggerParams(openTriggerProp, openTapCountProp, openTapTimeoutProp, openLongPressProp);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(openConsoleKeyProp,
                new GUIContent("Keyboard Shortcut", "Support quick activation when running in the Editor or using a physical keyboard."));
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Appearance & Layout", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (guiScaleProp.floatValue <= 0f) guiScaleProp.floatValue = 1f;
            if (refMinDimProp.floatValue <= 0f) refMinDimProp.floatValue = 1080f;
            if (guiOpacityProp.floatValue <= 0f) guiOpacityProp.floatValue = 0.95f;

            EditorGUILayout.Slider(guiOpacityProp, 0.1f, 1f, new GUIContent("Console Opacity"));
            EditorGUILayout.Slider(guiScaleProp, 0.5f, 3.0f, new GUIContent("GUI Scale Multiplier"));
            EditorGUILayout.PropertyField(refMinDimProp, new GUIContent("Reference Min Dimension"));
            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Memory Optimization", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (maxLogsProp.intValue <= 0) maxLogsProp.intValue = 200;
            EditorGUILayout.PropertyField(maxLogsProp, new GUIContent("Memory Pool Capacity"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Stack Trace Capture Rules", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(captureLogProp, new GUIContent("Info Logs"));
            if (captureLogProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "WARNING: Enabling Capture for Info Logs will continuously generate GC allocations (memory garbage). Extremely memory-intensive on mobile devices!",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(captureWarnProp, new GUIContent("Warning Logs"));
            EditorGUILayout.PropertyField(captureErrorProp, new GUIContent("Error & Exceptions"));

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            var enableProfilerProp = dataProperty.FindPropertyRelative("m_enableMiniProfiler");
            var profilerOpacityProp = dataProperty.FindPropertyRelative("m_profilerOpacity");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Mini Profiler (Dev-Only)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableProfilerProp,
                new GUIContent("Enable Mini Profiler", "Display the FPS & RAM bar at the top edge of the screen when Dev Mode is unlocked."));

            if (enableProfilerProp.boolValue)
            {
                EditorGUILayout.Slider(profilerOpacityProp, 0f, 1f, new GUIContent("Background Opacity", "Kéo về 0 để nền trong suốt hoàn toàn"));
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }
}