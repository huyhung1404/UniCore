using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.QuickAccess
{
    internal class QuickAccessAddGroupPopup : EditorWindow
    {
        private string _groupName = "New Group";

        public static void Open(Rect parentWindowRect)
        {
            var w = CreateInstance<QuickAccessAddGroupPopup>();
            var size = new Vector2(260, 70);
            var x = parentWindowRect.x + parentWindowRect.width - size.x;
            var y = parentWindowRect.y;
            w.position = new Rect(x, y, size.x, size.y);
            w.ShowPopup();
            w.Focus();
        }

        private void OnLostFocus() => Close();

        private void OnGUI()
        {
            PopupGUI.BeginPopup();
            using (new EditorGUILayout.VerticalScope())
            {
                _groupName = EditorGUILayout.TextField("Group Name", _groupName);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Create"))
                {
                    if (GroupNameValid(QuickAccessStorage.Database()))
                    {
                        QuickAccessStorage.Database().Groups.Add(new GroupData { GroupName = _groupName });
                        QuickAccessStorage.Save(QuickAccessStorage.Database());
                    }

                    Close();
                }
            }

            PopupGUI.EndPopup();
        }

        private bool GroupNameValid(QuickAccessDB db)
        {
            if (string.IsNullOrEmpty(_groupName))
            {
                Debug.LogError("Group name cannot be empty");
                return false;
            }

            var r = !db.Groups.Exists(g => g.GroupName == _groupName);
            if (!r) Debug.LogError($"Group name [{_groupName}] already exists");
            return r;
        }
    }
}