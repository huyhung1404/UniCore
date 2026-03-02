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
        public string NodeName => m_nodeName;

        public BaseAudioNode WithName(string name)
        {
            m_nodeName = name;
            return this;
        }

        public abstract UniTask<ClipData> GetClipData();
        public abstract (Log, string) IsValid();
    }
}
#endif