#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using UniCore.Audio.Node;
using UnityEngine.Audio;
using UniCore.Utilities;

namespace UniCore.Audio
{
    public class AudioSearchSystem
    {
        private AudioRuntimeSettings _runtimeSettings;
        private Dictionary<int, int> _nodeMap;
        private Dictionary<int, int> _configMap;

        public AudioSearchSystem WithRuntimeSettings(AudioRuntimeSettings settings)
        {
            if (settings == null) return this;
            _runtimeSettings = settings;

            var bakedNodes = settings.BakedNodes;
            if (bakedNodes != null)
            {
                _nodeMap = new Dictionary<int, int>(bakedNodes.Length);
                for (var i = 0; i < bakedNodes.Length; i++)
                {
                    _nodeMap[bakedNodes[i].HashKey] = i;
                }
            }

            var configs = settings.Configurations;
            if (configs != null)
            {
                _configMap = new Dictionary<int, int>(configs.Length);
                for (var i = 0; i < configs.Length; i++)
                {
                    if (string.IsNullOrEmpty(configs[i].Id)) continue;
                    _configMap[configs[i].Id.GetFNV1aHash()] = i;
                }
            }

            return this;
        }

        public BaseAudioNode FindNode(ReadOnlySpan<char> pathSpan)
        {
            if (_nodeMap == null || _runtimeSettings == null || pathSpan.IsEmpty) return null;

            var hash = pathSpan.GetFNV1aHash();
            return _nodeMap.TryGetValue(hash, out var index) ? _runtimeSettings.BakedNodes[index].Node : null;
        }

        public AudioConfiguration GetConfiguration(ReadOnlySpan<char> idSpan)
        {
            if (_configMap == null || _runtimeSettings == null || idSpan.IsEmpty) return null;

            var hash = idSpan.GetFNV1aHash();
            return _configMap.TryGetValue(hash, out var index) ? _runtimeSettings.Configurations[index] : null;
        }

        public static void BakeToRuntimeSettings(bool isEnabled, DirectionNode rootNode, AudioMixer mixer, SoundEmitter prefab, int poolSize, AudioConfiguration[] configs,
            AudioRuntimeSettings targetSettings)
        {
            if (targetSettings == null || rootNode == null) return;

            var bakedList = new List<AudioRuntimeSettings.AudioNodeEntry>();
            BakeNodeRecursive(rootNode, string.Empty, bakedList);

            targetSettings.Setup(isEnabled, bakedList.ToArray(), mixer, prefab, poolSize, configs);
        }

        private static void BakeNodeRecursive(BaseAudioNode node, string currentPath, List<AudioRuntimeSettings.AudioNodeEntry> list)
        {
            if (node == null) return;

            var nodePath = string.IsNullOrEmpty(currentPath) ? node.NodeName : $"{currentPath}/{node.NodeName}";
            var hash = nodePath.GetFNV1aHash();

            list.Add(new AudioRuntimeSettings.AudioNodeEntry
            {
                HashKey = hash,
                Node = node
            });

            if (node is DirectionNode directionNode && directionNode.Children != null)
            {
                foreach (var child in directionNode.Children)
                {
                    BakeNodeRecursive(child, nodePath, list);
                }
            }
        }
    }
}
#endif