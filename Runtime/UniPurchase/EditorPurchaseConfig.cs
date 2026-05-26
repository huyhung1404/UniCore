#if UNITY_EDITOR && ENABLE_UNI_PURCHASE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPurchase
{
    internal enum PurchaseConfigMode
    {
        Resources = 0,
        Addressable = 1,
        Instance = 2
    }

    [FilePath("ProjectSettings/UniCore_PurchaseSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class EditorPurchaseConfig : ScriptableSingleton<EditorPurchaseConfig>
    {
        [SerializeField] private bool m_isEnabled;
        [SerializeField] private PurchaseConfigMode m_configMode;
        [SerializeField] private List<ProductData> m_products = new List<ProductData>();
        [SerializeField] private PurchaseConfig m_instanceConfig;

        internal PurchaseConfigMode ConfigMode => m_configMode;

        internal void SaveData()
        {
            Save(true);
        }

        internal static PurchaseConfig CreateRuntimeInstance()
        {
            var editorConfig = instance;

            if (editorConfig == null)
            {
                Debug.LogError("[UniPurchase] EditorPurchaseConfig is empty.");
                return null;
            }

            if (editorConfig.m_configMode == PurchaseConfigMode.Instance)
            {
                Debug.LogError("[UniPurchase] Instance mode requires calling PurchaseService.SetConfig() before InitializeAsync().");
                return null;
            }

            var runtimeInstance = CreateInstance<PurchaseConfig>();
            runtimeInstance.SetUp(editorConfig.m_isEnabled, editorConfig.m_products);
            return runtimeInstance;
        }

        internal static void SetUpConfig(PurchaseConfig runtimeInstance)
        {
            if (runtimeInstance != instance.m_instanceConfig)
            {
                Debug.LogError("[UniPurchase] RuntimeInstance != InstanceConfig.");
            }

            SyncInstanceAsset();
        }

        internal static void SyncInstanceAsset()
        {
            var editorConfig = instance;
            if (editorConfig == null || editorConfig.m_instanceConfig == null) return;

            var target = editorConfig.m_instanceConfig;
            if (target.IsEnabled == editorConfig.m_isEnabled
                && IsProductListEqual(target.Products, editorConfig.m_products)) return;

            target.SetUp(editorConfig.m_isEnabled, editorConfig.m_products);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }

        private static bool IsProductListEqual(IReadOnlyList<ProductData> a, IReadOnlyList<ProductData> b)
        {
            if (a.Count != b.Count) return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
#endif