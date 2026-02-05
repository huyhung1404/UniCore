#if HAS_UNITASK && HAS_ADDRESSABLES
using UnityEngine;

namespace UniCore.Audio.Node
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Node/Single")]
    public class SingleAudioNode : BaseAudioNode
    {
        [SerializeField] private AudioClipReference reference;
    }
}
#endif