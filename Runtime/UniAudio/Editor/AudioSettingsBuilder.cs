#if HAS_UNITASK && HAS_ADDRESSABLES
using System.IO;
using UniCore.Audio;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UniCore.Editor.Audio
{
    public class AudioSettingsBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private const string k_resourceFolderPath = "Assets/Resources";

        public void OnPreprocessBuild(BuildReport report)
        {
            var path = $"Assets/Resources/{AudioRuntimeSettings.k_FileName}.asset";

            if (!Directory.Exists(k_resourceFolderPath))
            {
                Directory.CreateDirectory(k_resourceFolderPath);
            }

            var runtimeSettings = AudioEditorSettings.CreateRuntimeInstance();
            if (runtimeSettings == null) return;
            AssetDatabase.CreateAsset(runtimeSettings, path);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            var path = $"Assets/Resources/{AudioRuntimeSettings.k_FileName}.asset";
            if (!File.Exists(Path.GetFullPath(path))) return;
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }
    }
}
#endif