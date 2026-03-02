#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using UniCore.Audio.Node;
using UnityEngine;
using UnityEngine.Audio;

namespace UniCore.Audio
{
    public sealed class AudioRuntimeSettings : ScriptableObject
    {
        internal const string k_FileName = "UniCore_Runtime_AudioSettings";
        
        [Serializable]
        public struct AudioNodeEntry
        {
            public int HashKey;
            [SerializeReference] public BaseAudioNode Node;
        }

        [SerializeField] private AudioNodeEntry[] m_bakedNodes;
        [SerializeField] private AudioMixer m_outputMixer;
        [SerializeField] private SoundEmitter m_soundEmitterPrefab;
        [SerializeField] private int m_poolInitialSize = 16;
        [SerializeField] private AudioConfiguration[] m_configurations;
        
        public AudioNodeEntry[] BakedNodes => m_bakedNodes;
        public AudioMixer OutputMixer => m_outputMixer;
        public SoundEmitter SoundEmitterPrefab => m_soundEmitterPrefab;
        public int PoolInitialSize => m_poolInitialSize;
        public AudioConfiguration[] Configurations => m_configurations;

        public void Setup(AudioNodeEntry[] bakedNodes, AudioMixer mixer, SoundEmitter prefab, int poolSize, AudioConfiguration[] configs)
        {
            m_bakedNodes = bakedNodes;
            m_outputMixer = mixer;
            m_soundEmitterPrefab = prefab;
            m_poolInitialSize = poolSize;
            m_configurations = configs;
        }
    }
}
#endif