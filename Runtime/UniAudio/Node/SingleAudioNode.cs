#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Node/Single")]
    public class SingleAudioNode : BaseAudioNode
    {
        [SerializeField] private AudioClipReference reference;

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();
            data.clips.Add(await reference.LoadAsync());
            return data;
        }
    }
}
#endif