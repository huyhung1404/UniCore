#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class DirectionNode : BaseAudioNode
    {
        [SerializeReference] private List<BaseAudioNode> m_children = new List<BaseAudioNode>();
        public List<BaseAudioNode> Children => m_children;

        public override UniTask<ClipData> GetClipData()
        {
            throw new Exception("ContainerNode cannot be played directly.");
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (m_children == null || m_children.Count == 0) return (Log.Warning, "No child node.");

            for (var index = 0; index < m_children.Count; index++)
            {
                var child = m_children[index];
                if (child == null) continue;
                var childLog = child.IsValid();
                if (childLog.Item2 != null) return (childLog.Item1, $"[{index}] {childLog.Item2}");
            }

            return (Log.None, null);
        }
    }
}
#endif