#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class SequenceAudioNode : BaseAudioNode
    {
        [Serializable]
        public struct SequenceItem
        {
            public float DelayTime;
            public AudioClipReference Reference;
        }

        [SerializeField] private SequenceItem[] m_items = Array.Empty<SequenceItem>();

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();

            var tasks = new UniTask<AudioClip>[m_items.Length];
            for (var i = 0; i < m_items.Length; i++)
            {
                tasks[i] = m_items[i].Reference.LoadAsync();
            }

            var clips = await UniTask.WhenAll(tasks);

            for (var i = 0; i < m_items.Length; i++)
            {
                if (clips[i] != null)
                {
                    data.Commands.Add(new ClipPlayCommand
                    {
                        Clip = clips[i],
                        Delay = m_items[i].DelayTime,
                        Reference = m_items[i].Reference,
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
            if (m_items == null || m_items.Length == 0) return (Log.Warning, "Sequence is empty.");

            for (var i = 0; i < m_items.Length; i++)
            {
                var refClip = m_items[i].Reference;
                if (refClip == null || !refClip.RuntimeKeyIsValid()) return (Log.Error, $"[{i}] Reference is invalid.");
            }

            return (Log.None, null);
        }
    }
}
#endif