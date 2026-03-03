#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class CompositeAudioNode : BaseAudioNode
    {
        [SerializeField] private AudioClipReference[] m_references = Array.Empty<AudioClipReference>();

        public override async UniTask<ClipData> GetClipData(PlaySoundSignal signal)
        {
            var data = ClipDataPool.Pop();
            if (m_references == null || m_references.Length == 0) return data;

            var tasks = new UniTask<AudioClip>[m_references.Length];
            for (var i = 0; i < m_references.Length; i++)
            {
                tasks[i] = m_references[i].LoadAsync();
            }

            var clips = await UniTask.WhenAll(tasks);

            for (var i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    data.Commands.Add(new ClipPlayCommand 
                    { 
                        Clip = clips[i], 
                        Delay = 0f, 
                        Reference = m_references[i], 
                        ReleaseDelay = ReleaseDelay,
                        LayerIndex = 0,
                        LayerVolume = 1f
                    });
                }
            }
            return data;
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (m_references == null || m_references.Length == 0) return (Log.Warning, "References is empty.");
            return (Log.None, null);
        }
    }
}
#endif