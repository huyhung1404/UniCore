using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniCore.Editor.QuickAccess
{
    public class QuickAccessWindow : EditorWindow, IHasCustomMenu
    {
        private static GUIContent s_foldoutOff;
        private static GUIContent s_foldoutOn;
        private static GUIStyle s_headerBox;
        private static GUIStyle s_iconStyle;
        private string _search = "";

        [MenuItem("UniCore/Tools/Quick Access")]
        private static void Open() => GetWindow<QuickAccessWindow>("Quick Access");

        private static void InitStyle()
        {
            if (s_headerBox != null) return;

            s_headerBox = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12,
                padding = new RectOffset(6, 6, 4, 4)
            };

            s_iconStyle = new GUIStyle(EditorStyles.iconButton)
            {
                alignment = TextAnchor.MiddleCenter
            };

            s_foldoutOff = EditorGUIUtility.IconContent("IN Foldout");
            s_foldoutOn = EditorGUIUtility.IconContent("IN Foldout on");
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Add Group"), false, () => QuickAccessAddGroupPopup.Open(position));

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Copy Config"), false,
                () => EditorGUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(EditorStorage.Database(), Formatting.Indented));

            menu.AddItem(new GUIContent("Apply Config"), false, () => QuickAccessApplyPopup.Open(position));
        }

        private void OnGUI()
        {
            InitStyle();

            DrawToolbar();
            DrawFavorite();

            foreach (var g in EditorStorage.Database().Groups)
                DrawGroup(g);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _search = GUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSearchTextField") ?? GUI.skin.textField);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFavorite()
        {
            var favGUIDs = QuickAccessFavorite.GetFavorites();
            if (favGUIDs.Length == 0) return;
            if (!DrawFavoriteHeader()) return;
            var items = favGUIDs.Select(g => new AssetAddress { GuidAsset = g }).ToList();
            EditorAutoGrid.DrawGrid(items, _search, OnClick, static (a) => OnEdit(a, true));
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

            GUI.Box(rect, GUIContent.none, s_headerBox);


            var expand = EditorPrefs.GetBool("QuickAccess.Expand", true);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                expand = !expand;

            s_headerBox.alignment = TextAnchor.MiddleLeft;
            GUI.Label(rect, "⭐ Favorite", s_headerBox);
            s_headerBox.alignment = TextAnchor.MiddleCenter;
            EditorPrefs.SetBool("QuickAccess.Expand", expand);
            return expand;
        }

        private void DrawGroup(GroupData g)
        {
            var open = DrawGroupHeader(g, out var headerRect);

            HandleTitleDrop(headerRect, g);

            if (!open) return;

            if (g.Assets.Count == 0)
            {
                EditorGUILayout.HelpBox("Drag assets onto this header to add.", MessageType.Info);
                EditorGUILayout.Space(2);
                return;
            }

            EditorAutoGrid.DrawGrid(g.Assets, _search, OnClick, static (a) => OnEdit(a, false));
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

            GUI.Box(rect, GUIContent.none, s_headerBox);

            var arrowRect = new Rect(rect.x + 5, rect.y + 5, 20, rect.height);
            var labelRect = rect;
            var gearRect = new Rect(rect.xMax - 20, rect.y + 5, 20, rect.height);

            GUI.Label(labelRect, string.IsNullOrEmpty(group.GroupName) ? "Default" : group.GroupName, s_headerBox);

            var icon = group.GroupExpand ? s_foldoutOn : s_foldoutOff;
            if (GUI.Button(arrowRect, icon, s_iconStyle))
                group.GroupExpand = !group.GroupExpand;

            if (GUI.Button(gearRect, EditorGUIUtility.IconContent("_Popup"), s_iconStyle))
            {
                ShowGroupMenu(group);
            }

            return group.GroupExpand;
        }

        private static void ShowGroupMenu(GroupData group)
        {
            var menu = new GenericMenu();
            var index = EditorStorage.Database().Groups.IndexOf(group);

            var upEnable = index != -1 && index != 0;
            var downEnable = index != -1 && index != EditorStorage.Database().Groups.Count - 1;

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
            var removedGuids = group.Assets.Select(a => a.GuidAsset).ToList();
            EditorStorage.Database().Groups.Remove(group);
            EditorStorage.Database().Stats.RemoveAll(s => removedGuids.Contains(s.GUID));
            EditorStorage.Save(EditorStorage.Database());
        }

        private static void MoveGroup(int index, int newIndex)
        {
            var groups = EditorStorage.Database().Groups;
            (groups[index], groups[newIndex]) = (groups[newIndex], groups[index]);
            EditorStorage.Save(EditorStorage.Database());
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

                    if (group.Assets.All(a => a.GuidAsset != guid))
                        group.Assets.Add(new AssetAddress { GuidAsset = guid });
                }

                EditorStorage.Save(EditorStorage.Database());
                evt.Use();
            }
        }

        private static void OnClick(AssetAddress a)
        {
            var path = AssetDatabase.GUIDToAssetPath(a.GuidAsset);
            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);

            AssetDatabase.OpenAsset(obj);
            var fullName = obj.GetType().FullName;
            if (fullName != null && fullName.Contains("UnityEditor.DefaultAsset")) AssetDatabase.OpenAsset(obj);

            QuickAccessFavorite.RegisterUse(a.GuidAsset);
        }

        private static void OnEdit(AssetAddress a, bool isFavorite) => QuickAccessEditPopup.Open(a, isFavorite);
    }
}