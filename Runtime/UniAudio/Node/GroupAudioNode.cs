#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public abstract class GroupAudioNode : BaseAudioNode
    {
        [SerializeField] protected AudioClipReference[] m_references = Array.Empty<AudioClipReference>();
    }
}
#endif