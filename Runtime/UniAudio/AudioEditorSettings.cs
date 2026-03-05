#if UNITY_EDITOR && HAS_UNITASK && HAS_ADDRESSABLES
using UniCore.Audio.Node;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace UniCore.Audio
{
    [FilePath("ProjectSettings/UniCore_AudioSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class AudioEditorSettings : ScriptableSingleton<AudioEditorSettings>
    {
        [SerializeReference] private DirectionNode m_rootNode;
        [SerializeField] private AudioMixer m_outputMixer;
        [SerializeField] private SoundEmitter m_soundEmitterPrefab;
        [SerializeField] private int m_poolInitialSize = 16;
        [SerializeField] private AudioConfiguration[] m_configurations;
        [SerializeField] private bool m_isSystemEnabled;
        internal DirectionNode RootNode => m_rootNode;
        internal AudioMixer OutputMixer => m_outputMixer;
        internal SoundEmitter SoundEmitterPrefab => m_soundEmitterPrefab;
        internal int PoolInitialSize => m_poolInitialSize;
        internal AudioConfiguration[] Configurations => m_configurations;
        internal bool IsSystemEnabled => m_isSystemEnabled;
        
        internal void SaveData()
        {
            Save(true);
        }
        
        public static AudioRuntimeSettings CreateRuntimeInstance()
        {
            var editorSettings = instance;

            if (editorSettings == null || editorSettings.RootNode == null)
            {
                Debug.LogWarning("[UniAudio] AudioEditorSettings is empty.");
                return null;
            }

            var runtimeSettings = CreateInstance<AudioRuntimeSettings>();

            AudioSearchSystem.BakeToRuntimeSettings(
                editorSettings.IsSystemEnabled,
                editorSettings.RootNode,
                editorSettings.OutputMixer,
                editorSettings.SoundEmitterPrefab,
                editorSettings.PoolInitialSize,
                editorSettings.Configurations,
                runtimeSettings
            );

            return runtimeSettings;
        }
    }
}
#endif