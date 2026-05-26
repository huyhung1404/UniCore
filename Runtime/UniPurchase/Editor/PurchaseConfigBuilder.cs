#if ENABLE_UNI_PURCHASE
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if HAS_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace UniPurchase.Editor
{
    public class PurchaseConfigBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string k_resourceFolderPath = "Assets/Resources";
        private const string k_addressableFolderPath = "Assets/UniPurchase_Generated";

        public void OnPreprocessBuild(BuildReport report)
        {
            var mode = EditorPurchaseConfig.instance.ConfigMode;

            switch (mode)
            {
                case PurchaseConfigMode.Resources:
                    BuildResources();
                    break;
                case PurchaseConfigMode.Addressable:
                    BuildAddressable();
                    break;
                default:
                case PurchaseConfigMode.Instance:
                    break;
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var mode = EditorPurchaseConfig.instance.ConfigMode;

            switch (mode)
            {
                case PurchaseConfigMode.Resources:
                    CleanupResources();
                    break;
                case PurchaseConfigMode.Addressable:
                    CleanupAddressable();
                    break;
                default:
                case PurchaseConfigMode.Instance:
                    break;
            }
        }

        private static void BuildResources()
        {
            var path = $"{k_resourceFolderPath}/{PurchaseConfig.k_FileName}.asset";

            if (!Directory.Exists(k_resourceFolderPath))
            {
                Directory.CreateDirectory(k_resourceFolderPath);
            }

            var runtimeInstance = EditorPurchaseConfig.CreateRuntimeInstance();
            if (runtimeInstance == null) return;
            AssetDatabase.CreateAsset(runtimeInstance, path);
            AssetDatabase.SaveAssets();
        }

        private static void CleanupResources()
        {
            var path = $"{k_resourceFolderPath}/{PurchaseConfig.k_FileName}.asset";
            if (!File.Exists(Path.GetFullPath(path))) return;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        private static void BuildAddressable()
        {
#if HAS_ADDRESSABLES
            var path = $"{k_addressableFolderPath}/{PurchaseConfig.k_FileName}.asset";

            if (!Directory.Exists(k_addressableFolderPath))
            {
                Directory.CreateDirectory(k_addressableFolderPath);
            }

            var runtimeInstance = EditorPurchaseConfig.CreateRuntimeInstance();
            if (runtimeInstance == null) return;
            AssetDatabase.CreateAsset(runtimeInstance, path);
            AssetDatabase.SaveAssets();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[UniPurchase] Addressable Asset Settings not found. Please initialize Addressables via Window > Asset Management > Addressables > Groups.");
                return;
            }

            var guid = AssetDatabase.AssetPathToGUID(path);
            var group = settings.DefaultGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = PurchaseConfig.k_FileName;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
#else
            Debug.LogError("[UniPurchase] Addressable mode is selected but com.unity.addressables package is not installed.");
#endif
        }

        private static void CleanupAddressable()
        {
#if HAS_ADDRESSABLES
            var path = $"{k_addressableFolderPath}/{PurchaseConfig.k_FileName}.asset";

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                settings.RemoveAssetEntry(guid);
            }

            if (File.Exists(Path.GetFullPath(path)))
            {
                AssetDatabase.DeleteAsset(path);
            }

            if (Directory.Exists(k_addressableFolderPath) && Directory.GetFiles(k_addressableFolderPath).Length == 0)
            {
                AssetDatabase.DeleteAsset(k_addressableFolderPath);
            }

            AssetDatabase.Refresh();
#endif
        }
    }
}
#endif