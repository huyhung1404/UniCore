#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class SwitchAudioNode : BaseAudioNode
    {
        [Serializable]
        public struct SwitchItem
        {
            public int Key;
            public string Label;
            [SerializeReference] public BaseAudioNode Node;
        }

        [SerializeField] private List<SwitchItem> m_switches = new List<SwitchItem>();

        public override async UniTask<ClipData> GetClipData(PlaySoundSignal signal)
        {
            if (!signal.SwitchKey.HasValue) return null;
            foreach (var sw in m_switches)
            {
                if (sw.Key == signal.SwitchKey.Value && sw.Node != null)
                {
                    return await sw.Node.GetClipData(signal);
                }
            }
            return null;
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (m_switches.Count == 0) return (Log.Warning, "Switch list is empty.");
            
            for (var i = 0; i < m_switches.Count; i++)
            {
                if (m_switches[i].Node == null) return (Log.Error, $"Switch [{i}] has missing Node.");
            }
            return (Log.None, null);
        }
    }
}
#endif