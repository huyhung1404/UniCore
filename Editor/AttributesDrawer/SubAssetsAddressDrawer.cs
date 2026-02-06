using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UniCore.Utilities;

namespace UniCore.Editor
{
    [CustomPropertyDrawer(typeof(SubAssetsAddress))]
    public class SubAssetsAddressDrawer : PropertyDrawer
    {
        private const float HeaderH = 20f;
        private const float DropH = 50f;
        private const float ItemH = 18f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var parent = property.serializedObject.targetObject as ScriptableObject;
            if (parent == null) return 40;
            var count = GetSubAssets(parent).Count;
            var listHeight = count * (ItemH + 2);
            return HeaderH + Mathf.Max(DropH, listHeight) + 10;
        }

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            var parent = property.serializedObject.targetObject as ScriptableObject;
            if (parent == null)
            {
                EditorGUI.HelpBox(pos, "Use inside ScriptableObject", MessageType.Error);
                return;
            }

            SyncNames(property, parent);

            if (property.serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(parent);
            }

            GUI.Box(pos, GUIContent.none, EditorStyles.helpBox);

            var titleRect = new Rect(pos.x + 6, pos.y + 2, pos.width - HeaderH, HeaderH);
            EditorGUI.LabelField(titleRect, "Sub Assets", EditorStyles.boldLabel);

            var settingRect = new Rect(pos.xMax - HeaderH, pos.y + 4, HeaderH, HeaderH);
            if (GUI.Button(settingRect, EditorGUIUtility.IconContent("_Popup"), EditorStyles.iconButton))
            {
                ShowGroupMenu(property);
            }

            var bodyY = titleRect.yMax + 2;
            var bodyH = pos.height - HeaderH - 6;
            var colW = pos.width / 3f;

            var addRect = new Rect(pos.x + 4, bodyY, colW - 6, DropH);
            var listRect = new Rect(addRect.xMax + 4, bodyY, colW - 8, bodyH);
            var extractRect = new Rect(listRect.xMax + 4, bodyY, colW - 8, DropH);

            DrawAdd(addRect, parent);
            DrawList(listRect, parent);
            DrawExtract(extractRect);
        }

        private static void ShowGroupMenu(SerializedProperty property)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Log"), false, () => LogSubsJson(property));
            menu.ShowAsContext();
        }

        private static void LogSubsJson(SerializedProperty property)
        {
            var subsProp = property.FindPropertyRelative("subs");
            var list = new List<string>();

            for (var i = 0; i < subsProp.arraySize; i++)
            {
                list.Add(subsProp.GetArrayElementAtIndex(i).stringValue);
            }

            var json = JsonUtility.ToJson(new SubAssetsAddress(list.ToArray()), true);
            Debug.Log(json);
        }

        private static void DrawAdd(Rect rect, ScriptableObject parent)
        {
            GUI.Box(rect, "Drag Asset Here\n→ SubAsset", EditorStyles.helpBox);
            HandleAddDrag(rect, parent);
        }

        private static void HandleAddDrag(Rect rect, ScriptableObject parent)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }

            if (evt.type != EventType.DragPerform) return;
            DragAndDrop.AcceptDrag();

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (!AssetDatabase.Contains(obj)) continue;
                if (AssetDatabase.IsSubAsset(obj)) continue;

                ConvertToSubAsset(obj, parent);
            }

            evt.Use();
        }

        private static void ConvertToSubAsset(Object obj, ScriptableObject parent)
        {
            var objPath = AssetDatabase.GetAssetPath(obj);
            var parentPath = AssetDatabase.GetAssetPath(parent);

            AssetDatabase.StartAssetEditing();

            var clone = Object.Instantiate(obj);
            clone.name = obj.name;

            AssetDatabase.AddObjectToAsset(clone, parent);
            AssetDatabase.DeleteAsset(objPath);

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(parentPath);
        }

        private static void DrawList(Rect rect, ScriptableObject parent)
        {
            var y = rect.y;

            foreach (var sub in GetSubAssets(parent))
            {
                var r = new Rect(rect.x + 4, y, rect.width - 8, ItemH);

                EditorGUI.ObjectField(r, sub, typeof(Object), false);

                HandleItemDrag(r, sub);

                y += ItemH + 2;
            }
        }

        private static void HandleItemDrag(Rect rect, Object sub)
        {
            var evt = Event.current;

            if (evt.type == EventType.MouseDrag && rect.Contains(evt.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { sub };
                DragAndDrop.StartDrag("SubAssetDrag");
                evt.Use();
            }
        }

        private static void DrawExtract(Rect rect)
        {
            GUI.Box(rect, "Drag SubAsset Here\n→ Extract", EditorStyles.helpBox);
            HandleRemoveDrop(rect);
        }

        private static void HandleRemoveDrop(Rect rect)
        {
            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                evt.Use();
            }

            if (evt.type != EventType.DragPerform) return;
            DragAndDrop.AcceptDrag();

            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj == null) continue;
                if (!AssetDatabase.IsSubAsset(obj)) continue;

                ExtractSubAsset(obj);
            }

            evt.Use();
        }

        public static void ExtractSubAsset(Object sub)
        {
            var parentPath = AssetDatabase.GetAssetPath(sub);
            var parentDir = Path.GetDirectoryName(parentPath);
            if (parentDir != null)
            {
                var newPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(parentDir, sub.name + ".asset"));

                AssetDatabase.StartAssetEditing();

                var clone = Object.Instantiate(sub);
                clone.name = sub.name;

                AssetDatabase.CreateAsset(clone, newPath);
            }

            Object.DestroyImmediate(sub, true);

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static List<Object> GetSubAssets(ScriptableObject parent)
        {
            var path = AssetDatabase.GetAssetPath(parent);
            var all = AssetDatabase.LoadAllAssetsAtPath(path);

            var list = new List<Object>();
            foreach (var a in all)
                if (a != parent && AssetDatabase.IsSubAsset(a))
                    list.Add(a);
            return list;
        }

        private static void SyncNames(SerializedProperty prop, ScriptableObject parent)
        {
            var arr = prop.FindPropertyRelative("subs");
            var subs = GetSubAssets(parent);

            arr.arraySize = subs.Count;
            for (var i = 0; i < subs.Count; i++)
                arr.GetArrayElementAtIndex(i).stringValue = subs[i].name;
        }
    }
}