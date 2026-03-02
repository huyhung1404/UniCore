using UniCore.Storage;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SettingsProvider = UniCore.Storage.SettingsProvider;

namespace UniCore.Editor.Storage
{
    public sealed class StorageBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var assetPath = $"Assets/Resources/{SettingsProvider.k_FileName}.asset";
            if (report == null) return;

            var editorConfig = StorageEditorConfig.instance;
            var runtimeSO = ScriptableObject.CreateInstance<StorageSettings>();
            
            runtimeSO.Data = editorConfig.EditorData;
            
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            AssetDatabase.CreateAsset(runtimeSO, assetPath);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var assetPath = $"Assets/Resources/{SettingsProvider.k_FileName}.asset";
            
            if (report == null) return;
            
            var asset = AssetDatabase.LoadAssetAtPath<StorageSettings>(assetPath);
            if (asset == null) return;
            
            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}