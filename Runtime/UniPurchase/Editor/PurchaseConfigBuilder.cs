#if ENABLE_UNI_PURCHASE
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UniPurchase.Editor
{
    public class PurchaseConfigBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string k_resourceFolderPath = "Assets/Resources";

        public void OnPreprocessBuild(BuildReport report)
        {
            var path = $"Assets/Resources/{PurchaseConfig.k_FileName}.asset";

            if (!Directory.Exists(k_resourceFolderPath))
            {
                Directory.CreateDirectory(k_resourceFolderPath);
            }

            var runtimeInstance = EditorPurchaseConfig.CreateRuntimeInstance();
            if (runtimeInstance == null) return;
            AssetDatabase.CreateAsset(runtimeInstance, path);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var path = $"Assets/Resources/{PurchaseConfig.k_FileName}.asset";
            if (!File.Exists(Path.GetFullPath(path))) return;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }
    }
}
#endif