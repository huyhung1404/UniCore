using UnityEditor;
using UnityEngine;
#if ENABLE_UNI_PURCHASE

#endif

namespace UniPurchase.Editor
{
    public static class PurchaseConfigProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/UniCore/Purchasing", SettingsScope.Project)
            {
                keywords = new[] { "Purchase", "UniCore", "Purchasing" },
                guiHandler = (_) =>
                {
#if ENABLE_UNI_PURCHASE
                    var config = EditorPurchaseConfig.instance;
                    if (config == null) return;

                    var serializedObject = new SerializedObject(config);
                    serializedObject.Update();
#endif
                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(5, 5, 5, 5) });
                    EditorGUI.BeginChangeCheck();

#if ENABLE_UNI_PURCHASE
                    OnGUI(serializedObject);
#else
                    DrawMissingDependenciesWarning();
#endif
#if ENABLE_UNI_PURCHASE
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        config.SaveData();
                    }
#endif
                    EditorGUILayout.Space(20);
                    EditorGUILayout.EndVertical();
                }
            };
            return provider;
        }

#if ENABLE_UNI_PURCHASE
        private static void OnGUI(SerializedObject serializedObject)
        {
            var enableProp = serializedObject.FindProperty("m_isEnabled");
            var isSystemEnabled = DrawMasterToggle(enableProp);
            if (!isSystemEnabled) return;
        }

        public static bool DrawMasterToggle(SerializedProperty enableProp)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(5);
                enableProp.boolValue = DrawSwitchToggle(enableProp.boolValue);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(15);
            return enableProp.boolValue;
        }

        public static bool DrawSwitchToggle(bool value)
        {
            var rect = GUILayoutUtility.GetRect(50, 24);
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                value = !value;
                GUI.changed = true;
                e.Use();
            }

            if (e.type != EventType.Repaint) return value;
            var bgColor = value ? new Color(0.2f, 0.84f, 0.29f) : new Color(0.45f, 0.45f, 0.45f);

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, bgColor, 0, rect.height / 2f);

            const float padding = 2f;
            var knobSize = rect.height - padding * 2f;

            var knobX = value ? (rect.x + rect.width - knobSize - padding) : (rect.x + padding);
            var knobRect = new Rect(knobX, rect.y + padding, knobSize, knobSize);

            var shadowRect = new Rect(knobRect.x, knobRect.y + 1.5f, knobSize, knobSize);
            GUI.DrawTexture(shadowRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.35f), 0, knobSize / 2f);
            GUI.DrawTexture(knobRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, knobSize / 2f);

            return value;
        }
#else
        private static void DrawMissingDependenciesWarning()
        {
            var warningStyle = new GUIStyle("helpbox")
            {
                fontSize = 13,
                padding = new RectOffset(5, 5, 10, 0),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            EditorGUILayout.BeginVertical(warningStyle);

            GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), new GUIStyle { alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.Space(5);

            GUILayout.Label("MISSING DEPENDENCIES!", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
            EditorGUILayout.Space(10);

            GUILayout.Label("The UniPurchase system requires the following packages to function:", EditorStyles.label);
            EditorGUILayout.Space(5);
            GUILayout.Label("• com.unity.purchasing (>= 5.0.0)", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();
        }
#endif
    }
}