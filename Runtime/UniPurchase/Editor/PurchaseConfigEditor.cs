#if ENABLE_UNI_PURCHASE
using UnityEditor;
using UnityEngine;

namespace UniPurchase.Editor
{
    [CustomEditor(typeof(PurchaseConfig))]
    public class PurchaseConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "PurchaseConfig is managed via Project Settings.",
                MessageType.Info);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("Open Purchasing Settings", GUILayout.Height(30)))
            {
                SettingsService.OpenProjectSettings("Project/UniCore/Purchasing");
            }
        }
    }
}
#endif
