#if UNITY_EDITOR && ENABLE_UNI_PURCHASE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPurchase
{
    [FilePath("ProjectSettings/UniCore_PurchaseSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class EditorPurchaseConfig : ScriptableSingleton<EditorPurchaseConfig>
    {
        [SerializeField] private bool m_isEnabled;
        [SerializeField] private List<ProductData> m_products = new List<ProductData>();

        internal void SaveData()
        {
            Save(true);
        }

        internal static PurchaseConfig CreateRuntimeInstance()
        {
            var editorConfig = instance;

            if (editorConfig == null)
            {
                Debug.LogWarning("[UniPurchase] EditorPurchaseConfig is empty.");
                return null;
            }

            var runtimeInstance = CreateInstance<PurchaseConfig>();
            runtimeInstance.SetUp(editorConfig.m_isEnabled, editorConfig.m_products);
            return runtimeInstance;
        }
    }
}
#endif