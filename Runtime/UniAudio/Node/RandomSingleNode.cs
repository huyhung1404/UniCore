#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class RandomSingleAudioNode : GroupAudioNode
    {
        public enum SequenceMode
        {
            Random,
            RandomNoImmediateRepeat,
            Sequential,
        }

        [SerializeField] private SequenceMode m_sequenceMode = SequenceMode.RandomNoImmediateRepeat;
        private int _nextClipToPlay = -1;
        private int _lastClipPlayed = -1;

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();
            var reference = GetNextClip();
            var clip = await reference.LoadAsync();

            if (clip != null)
            {
                data.Commands.Add(new ClipPlayCommand
                {
                    Clip = clip,
                    Delay = 0f,
                    Reference = reference,
                    ReleaseDelay = ReleaseDelay
                });
            }
            
            return data;
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (_references == null) return (Log.Error, "References array is null.");
            if (_references.Length == 0) return (Log.Error, "References array is empty.");
            
            for (var index = 0; index < _references.Length; index++)
            {
                var refClip = _references[index];
                if (refClip == null || !refClip.RuntimeKeyIsValid()) return (Log.Error, $"[{index}] Reference is invalid.");
            }

            return (Log.None, null);
        }

        private AudioClipReference GetNextClip()
        {
            if (_references.Length == 1) return _references[0];

            if (_nextClipToPlay == -1)
            {
                _nextClipToPlay = m_sequenceMode == SequenceMode.Sequential ? 0 : Random.Range(0, _references.Length);
            }
            else
            {
                switch (m_sequenceMode)
                {
                    default:
                    case SequenceMode.Random:
                        _nextClipToPlay = Random.Range(0, _references.Length);
                        break;

                    case SequenceMode.RandomNoImmediateRepeat:
                        do
                        {
                            _nextClipToPlay = Random.Range(0, _references.Length);
                        } while (_nextClipToPlay == _lastClipPlayed);

                        break;

                    case SequenceMode.Sequential:
                        _nextClipToPlay = (int)Mathf.Repeat(++_nextClipToPlay, _references.Length);
                        break;
                }
            }

            _lastClipPlayed = _nextClipToPlay;

            return _references[_nextClipToPlay];
        }
    }
}
#endif