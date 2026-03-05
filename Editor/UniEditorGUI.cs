#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor
{
    public static class UniEditorGUI
    {
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
    }
}
#endif