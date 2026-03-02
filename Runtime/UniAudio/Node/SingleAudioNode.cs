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
        [SerializeField] private AudioClipReference _reference;

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();
            data.Clips.Add(await _reference.LoadAsync());
            return data;
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (_reference == null) return (Log.Error, "References is null.");
            if (!_reference.RuntimeKeyIsValid()) return (Log.Error, "Reference is invalid.");
            return (Log.None, null);
        }
    }
}
#endif