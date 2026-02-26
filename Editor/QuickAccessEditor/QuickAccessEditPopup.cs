using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.QuickAccess
{
    public class QuickAccessEditPopup : EditorWindow
    {
        private AssetAddress _asset;
        private Color _colorValue;
        private bool _isFavorite;

        public static void Open(AssetAddress a, bool isFav)
        {
            var w = CreateInstance<QuickAccessEditPopup>();
            w._asset = a;
            w._isFavorite = isFav;
            var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            var size = new Vector2(260, 60);
            w.position = new Rect(mousePos.x, mousePos.y, size.x, size.y);
            w.ShowPopup();
        }

        public void OnLostFocus() => Close();

        private void OnGUI()
        {
            PopupGUI.BeginPopup();

            var db = QuickAccessStorage.Database();
            
            EditorGUI.BeginChangeCheck();
            _asset.Name = EditorGUILayout.TextField("Name", _asset.Name);
            if (EditorGUI.EndChangeCheck())
            {
                QuickAccessStorage.Save(db);
                RepaintAll();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Delete"))
            {
                if (_isFavorite)
                {
                    db.Stats.RemoveAll(s => s.GUID == _asset.GuidAsset);
                }
                else
                {
                    foreach (var g in db.Groups)
                        g.Assets.Remove(_asset);

                    db.Stats.RemoveAll(s => s.GUID == _asset.GuidAsset);
                }

                QuickAccessStorage.Save(db);
                Close();
            }

            PopupGUI.EndPopup();
        }

        private static void RepaintAll()
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<QuickAccessWindow>()) w.Repaint();
        }
    }
}