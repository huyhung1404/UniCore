#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UniCore.Audio.Node
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Node/Random Single")]
    public class RandomSingleAudioNode : GroupAudioNode
    {
        public enum SequenceMode
        {
            Random,
            RandomNoImmediateRepeat,
            Sequential,
        }

        [SerializeField] private SequenceMode _sequenceMode = SequenceMode.RandomNoImmediateRepeat;
        private int _nextClipToPlay = -1;
        private int _lastClipPlayed = -1;

        private void OnEnable()
        {
            hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();
            var reference = GetNextClip();
            data.Clips.Add(await reference.LoadAsync());
            return data;
        }

        private AudioClipReference GetNextClip()
        {
            if (_references.Length == 1) return _references[0];

            if (_nextClipToPlay == -1)
            {
                _nextClipToPlay = _sequenceMode == SequenceMode.Sequential ? 0 : Random.Range(0, _references.Length);
            }
            else
            {
                switch (_sequenceMode)
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