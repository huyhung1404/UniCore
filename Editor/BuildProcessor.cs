using UniCore.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UniCore.Editor
{
    public abstract class BuildProcessor<T1, T2, T3> : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        where T1 : UniSettingsBase<T1, T2, T3>
        where T3 : UniSettingsEditorBase<T3, T2>
    {
        public int callbackOrder => 0;

        public abstract string FileName();

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report == null) return;

            var instance = Resources.Load<T1>(FileName());
            if (instance != null) return;

            var assetPath = $"Assets/Resources/{FileName()}.asset";

            var editorConfig = UniSettingsEditorBase<T3, T2>.instance;
            var runtimeSO = ScriptableObject.CreateInstance<T1>();

            runtimeSO.SetData(editorConfig.EditorData);

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            AssetDatabase.CreateAsset(runtimeSO, assetPath);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var assetPath = $"Assets/Resources/{FileName()}.asset";

            if (report == null) return;

            var asset = AssetDatabase.LoadAssetAtPath<T1>(assetPath);
            if (asset == null) return;

            AssetDatabase.DeleteAsset(assetPath);
        }
    }
}