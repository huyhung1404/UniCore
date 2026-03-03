#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class LayeredAudioNode : BaseAudioNode
    {
        [Serializable]
        public struct LayerItem
        {
            public string LayerName; 
            [Range(0f, 1f)] public float InitialVolume;
            public AudioClipReference Reference;
        }
        
        [SerializeField] private LayerItem[] m_layers = Array.Empty<LayerItem>();

        public override async UniTask<ClipData> GetClipData(PlaySoundSignal signal)
        {
            var data = ClipDataPool.Pop();
            if (m_layers == null || m_layers.Length == 0) return data;
            var tasks = new UniTask<AudioClip>[m_layers.Length];
            for (var i = 0; i < m_layers.Length; i++)
            {
                tasks[i] = m_layers[i].Reference.LoadAsync();
            }

            var clips = await UniTask.WhenAll(tasks);

            for (var i = 0; i < m_layers.Length; i++)
            {
                var clip = clips[i];
                if (clip == null) continue;
                
                data.Commands.Add(new ClipPlayCommand 
                { 
                    Clip = clip, 
                    Delay = 0f,
                    Reference = m_layers[i].Reference,
                    ReleaseDelay = ReleaseDelay,
                    LayerIndex = i,
                    LayerVolume = m_layers[i].InitialVolume
                });
            }
            return data;
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (m_layers == null || m_layers.Length == 0) return (Log.Warning, "Layers array is empty.");
            
            for (var i = 0; i < m_layers.Length; i++)
            {
                var refClip = m_layers[i].Reference;
                if (refClip == null || !refClip.RuntimeKeyIsValid()) return (Log.Error, $"[Layer {i}] Reference is invalid.");
            }
            return (Log.None, null);
        }
    }
}
#endif