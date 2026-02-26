using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.QuickAccess
{
    internal static class PopupGUI
    {
        private static GUIStyle s_bgStyle;

        public static void BeginPopup()
        {
            if (s_bgStyle == null)
            {
                s_bgStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(8, 8, 8, 8)
                };
            }

            var rect = new Rect(0, 0, Screen.width, Screen.height);

            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));

            Handles.BeginGUI();
            Handles.color = new Color(0, 0, 0, 0.6f);
            Handles.DrawAAPolyLine(2f,
                new Vector3(0, 0),
                new Vector3(Screen.width, 0),
                new Vector3(Screen.width, Screen.height),
                new Vector3(0, Screen.height),
                new Vector3(0, 0));
            Handles.EndGUI();

            GUILayout.BeginVertical(s_bgStyle);
        }

        public static void EndPopup()
        {
            GUILayout.EndVertical();
        }
    }
}