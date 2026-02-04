using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniCore.Editor.QuickAccess
{
    public class QuickAccessWindow : EditorWindow, IHasCustomMenu
    {
        private static GUIContent foldoutOff;
        private static GUIContent foldoutOn;
        private static GUIStyle headerBox;
        private static GUIStyle iconStyle;
        private string search = "";

        [MenuItem("UniCore/Tools/Quick Access")]
        private static void Open() => GetWindow<QuickAccessWindow>("Quick Access");

        private static void InitStyle()
        {
            if (headerBox != null) return;

            headerBox = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset(6, 6, 4, 4)
            };

            iconStyle = new GUIStyle(EditorStyles.iconButton)
            {
                alignment = TextAnchor.MiddleCenter
            };

            foldoutOff = EditorGUIUtility.IconContent("IN Foldout");
            foldoutOn = EditorGUIUtility.IconContent("IN Foldout on");
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Add Group"), false, () => QuickAccessAddGroupPopup.Open(position));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Copy Config"), false,
                () => EditorGUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(QuickAccessStorage.Database(), Formatting.Indented));

            menu.AddItem(new GUIContent("Apply Config"), false, () => QuickAccessApplyPopup.Open(position));
        }

        private void OnGUI()
        {
            InitStyle();

            DrawToolbar();
            DrawFavorite();

            foreach (var g in QuickAccessStorage.Database().groups)
                DrawGroup(g);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.textField);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFavorite()
        {
            var favGUIDs = QuickAccessFavorite.GetFavorites();
            if (favGUIDs.Length == 0) return;
            if (!DrawFavoriteHeader()) return;
            var items = favGUIDs.Select(g => new AssetAddress { guidAsset = g }).ToList();
            EditorAutoGrid.DrawGrid(items, search, OnClick, static (a) => OnEdit(a, true));
        }

        private static bool DrawFavoriteHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            rect.y += 2;
            rect.x += 2;
            rect.height -= 2;
            rect.width -= 4;

            var isHover = rect.Contains(Event.current.mousePosition) && (Event.current.type == EventType.DragUpdated ||
                                                                         Event.current.type == EventType.DragPerform);

            var bg = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, isHover ? 1f : 0.9f)
                : new Color(0.85f, 0.85f, 0.85f, isHover ? 1f : 0.9f);

            EditorGUI.DrawRect(rect, bg);

            GUI.Box(rect, GUIContent.none, headerBox);


            var expand = EditorPrefs.GetBool("QuickAccess.Expand", true);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                expand = !expand;

            headerBox.alignment = TextAnchor.MiddleLeft;
            GUI.Label(rect, "⭐ Favorite", headerBox);
            headerBox.alignment = TextAnchor.MiddleCenter;
            EditorPrefs.SetBool("QuickAccess.Expand", expand);
            return expand;
        }

        private void DrawGroup(GroupData g)
        {
            var open = DrawGroupHeader(g, out var headerRect);

            HandleTitleDrop(headerRect, g);

            if (!open) return;

            if (g.assets.Count == 0)
            {
                EditorGUILayout.HelpBox("Drag assets onto this header to add.", MessageType.Info);
                EditorGUILayout.Space(2);
                return;
            }

            EditorAutoGrid.DrawGrid(g.assets, search, OnClick, static (a) => OnEdit(a, false));
        }

        private static bool DrawGroupHeader(GroupData group, out Rect rect)
        {
            rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            rect.y += 2;
            rect.x += 2;
            rect.height -= 2;
            rect.width -= 4;

            var isHover = rect.Contains(Event.current.mousePosition) && (Event.current.type == EventType.DragUpdated ||
                                                                         Event.current.type == EventType.DragPerform);

            var bg = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f, isHover ? 1f : 0.9f)
                : new Color(0.85f, 0.85f, 0.85f, isHover ? 1f : 0.9f);

            EditorGUI.DrawRect(rect, bg);

            GUI.Box(rect, GUIContent.none, headerBox);

            var arrowRect = new Rect(rect.x + 5, rect.y + 5, 20, rect.height);
            var labelRect = rect;
            var gearRect = new Rect(rect.xMax - 20, rect.y + 5, 20, rect.height);

            GUI.Label(labelRect, string.IsNullOrEmpty(group.groupName) ? "Default" : group.groupName, headerBox);

            var icon = group.groupExpand ? foldoutOn : foldoutOff;
            if (GUI.Button(arrowRect, icon, iconStyle))
                group.groupExpand = !group.groupExpand;

            if (GUI.Button(gearRect, EditorGUIUtility.IconContent("_Popup"), iconStyle))
            {
                ShowGroupMenu(group);
            }

            return group.groupExpand;
        }

        private static void ShowGroupMenu(GroupData group)
        {
            var menu = new GenericMenu();
            var index = QuickAccessStorage.Database().groups.IndexOf(group);

            var upEnable = index != -1 && index != 0;
            var downEnable = index != -1 && index != QuickAccessStorage.Database().groups.Count - 1;

            AddItem(menu, new GUIContent("Move Up"), upEnable, () => MoveGroup(index, index - 1));
            AddItem(menu, new GUIContent("Move Down"), downEnable, () => MoveGroup(index, index + 1));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Group"), false, () => DeleteGroup(group));
            menu.ShowAsContext();
        }

        private static void AddItem(GenericMenu menu, GUIContent content, bool enable, GenericMenu.MenuFunction callback)
        {
            if (enable)
            {
                menu.AddItem(content, false, callback);
                return;
            }

            menu.AddDisabledItem(content);
        }

        private static void DeleteGroup(GroupData group)
        {
            var removedGuids = group.assets.Select(a => a.guidAsset).ToList();
            QuickAccessStorage.Database().groups.Remove(group);
            QuickAccessStorage.Database().stats.RemoveAll(s => removedGuids.Contains(s.guid));
            QuickAccessStorage.Save(QuickAccessStorage.Database());
        }

        private static void MoveGroup(int index, int newIndex)
        {
            var groups = QuickAccessStorage.Database().groups;
            (groups[index], groups[newIndex]) = (groups[newIndex], groups[index]);
            QuickAccessStorage.Save(QuickAccessStorage.Database());
        }

        private static void HandleTitleDrop(Rect rect, GroupData group)
        {
            var evt = Event.current;

            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
                return;
            }

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    var path = AssetDatabase.GetAssetPath(obj);
                    var guid = AssetDatabase.AssetPathToGUID(path);

                    if (group.assets.All(a => a.guidAsset != guid))
                        group.assets.Add(new AssetAddress { guidAsset = guid });
                }

                QuickAccessStorage.Save(QuickAccessStorage.Database());
                evt.Use();
            }
        }

        private static void OnClick(AssetAddress a)
        {
            var path = AssetDatabase.GUIDToAssetPath(a.guidAsset);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);

            AssetDatabase.OpenAsset(obj);
            var fullName = obj.GetType().FullName;
            if (fullName != null && fullName.Contains("UnityEditor.DefaultAsset")) AssetDatabase.OpenAsset(obj);

            QuickAccessFavorite.RegisterUse(a.guidAsset);
        }

        private static void OnEdit(AssetAddress a, bool isFavorite) => QuickAccessEditPopup.Open(a, isFavorite);
    }
}