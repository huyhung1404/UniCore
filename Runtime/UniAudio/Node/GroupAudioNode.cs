#if HAS_UNITASK && HAS_ADDRESSABLES
using UnityEngine;

namespace UniCore.Audio.Node
{
    public abstract class GroupAudioNode : BaseAudioNode
    {
        [SerializeField] protected AudioClipReference[] _references;
    }
}
#endif