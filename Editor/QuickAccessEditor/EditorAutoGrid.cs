using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.QuickAccess
{
    internal static class EditorAutoGrid
    {
        private const float k_menuWidth = 18f;
        private const float k_width = 100f;
        private const float k_height = 21f;
        private static readonly GUIContent s_moreIcon = EditorGUIUtility.IconContent("_Menu");
        private static GUIStyle s_gridBgStyle;
        private static GUIStyle s_buttonStyle;
        private static GUIStyle s_labelStyle;

        private static void InitGridStyle()
        {
            if (s_gridBgStyle != null) return;

            s_gridBgStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 6, 6),
                margin = new RectOffset(4, 4, 0, 4)
            };

            s_buttonStyle = new GUIStyle("Button");

            s_labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                wordWrap = false
            };
        }

        public static void DrawGrid(List<AssetAddress> items, string search,
            System.Action<AssetAddress> onClick,
            System.Action<AssetAddress> onEdit)
        {
            InitGridStyle();

            if (!string.IsNullOrEmpty(search)) items = items.Where(i => GetLabel(i).ToLower().Contains(search.ToLower())).ToList();

            EditorGUILayout.BeginVertical(s_gridBgStyle);

            var width = EditorGUIUtility.currentViewWidth - 32f;
            var columns = Mathf.Max(1, Mathf.FloorToInt(width / k_width));
            var rows = Mathf.CeilToInt(items.Count / (float)columns);

            for (var r = 0; r < rows; r++)
            {
                var rowRect = GUILayoutUtility.GetRect(0, k_height);

                const float spacing = 4f;
                var itemWidth = (rowRect.width - (columns - 1) * spacing) / columns;

                for (var c = 0; c < columns; c++)
                {
                    var index = r * columns + c;
                    if (index >= items.Count) break;

                    var rect = new Rect(rowRect.x + c * (itemWidth + spacing), rowRect.y, itemWidth, k_height);
                    DrawSplitItem(rect, items[index], onClick, onEdit);
                }

                if (r < rows - 1) GUILayout.Space(spacing);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawSplitItem(Rect rect, AssetAddress a,
            System.Action<AssetAddress> onMainClick,
            System.Action<AssetAddress> onMenuClick)
        {
            var evt = Event.current;

            GUI.Box(rect, GUIContent.none, s_buttonStyle);

            var mainRect = new Rect(rect.x, rect.y, rect.width - k_menuWidth, rect.height);
            var menuRect = new Rect(rect.x + rect.width - k_menuWidth, rect.y, k_menuWidth, rect.height);

            var fullLabel = GetAssetLabel(a);

            var truncated = Truncate(string.IsNullOrEmpty(a.Name) ? fullLabel : a.Name, s_labelStyle, mainRect.width - 6);
            GUI.Label(mainRect, new GUIContent(truncated, fullLabel), s_labelStyle);

            GUI.Label(menuRect, s_moreIcon, new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter
            });

            if (rect.Contains(evt.mousePosition))
                EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.04f));

            if (evt.type == EventType.MouseDown && rect.Contains(evt.mousePosition))
            {
                if (menuRect.Contains(evt.mousePosition))
                    onMenuClick?.Invoke(a);
                else
                    onMainClick?.Invoke(a);

                evt.Use();
            }
        }

        private static string GetLabel(AssetAddress a)
        {
            var path = AssetDatabase.GUIDToAssetPath(a.GuidAsset);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            return string.IsNullOrEmpty(a.Name) ? obj?.name ?? "Missing" : a.Name;
        }

        private static string GetAssetLabel(AssetAddress a)
        {
            var path = AssetDatabase.GUIDToAssetPath(a.GuidAsset);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            return obj?.name ?? "Missing";
        }

        private static string Truncate(string text, GUIStyle style, float width)
        {
            if (style.CalcSize(new GUIContent(text)).x <= width) return text;

            while (text.Length > 0 && style.CalcSize(new GUIContent(text + "...")).x > width)
                text = text.Substring(0, text.Length - 1);

            return text + "...";
        }
    }
}