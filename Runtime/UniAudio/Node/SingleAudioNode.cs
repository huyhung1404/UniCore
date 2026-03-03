#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class SingleAudioNode : BaseAudioNode
    {
        [SerializeField] private AudioClipReference m_reference;

        public override async UniTask<ClipData> GetClipData(PlaySoundSignal signal)
        {
            var data = ClipDataPool.Pop();
            var clip = await m_reference.LoadAsync();
            if (clip != null)
            {
                data.Commands.Add(new ClipPlayCommand
                {
                    Clip = clip,
                    Delay = 0f,
                    Reference = m_reference,
                    ReleaseDelay = ReleaseDelay,
                    LayerIndex = 0,
                    LayerVolume = 1f
                });
            }

            return data;
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (m_reference == null) return (Log.Error, "Reference is null.");
            if (!m_reference.RuntimeKeyIsValid()) return (Log.Error, "Reference is invalid Addressable key.");

            return (Log.None, null);
        }
    }
}
#endif