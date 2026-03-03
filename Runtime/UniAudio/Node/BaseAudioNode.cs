#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniCore.Audio.Node
{
    public enum Log
    {
        None,
        Warning,
        Error
    }

    [Serializable]
    public abstract class BaseAudioNode
    {
        [SerializeField] private string m_nodeName;
        [SerializeField, Min(0)] private int m_maxInstances; 
        [SerializeField, Min(0)] private float m_releaseDelay = 15f; 

        public string NodeName => m_nodeName;
        public int MaxInstances => m_maxInstances;
        public float ReleaseDelay => m_releaseDelay;

        public BaseAudioNode WithName(string name)
        {
            m_nodeName = name;
            return this;
        }

        public abstract UniTask<ClipData> GetClipData(PlaySoundSignal signal);
        public abstract (Log, string) IsValid();
    }
}
#endif