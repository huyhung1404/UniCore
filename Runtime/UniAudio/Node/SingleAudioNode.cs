#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Node/Single")]
    public class SingleAudioNode : BaseAudioNode
    {
        [SerializeField] private AudioClipReference _reference;

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();
            data.Clips.Add(await _reference.LoadAsync());
            return data;
        }
    }
}
#endif