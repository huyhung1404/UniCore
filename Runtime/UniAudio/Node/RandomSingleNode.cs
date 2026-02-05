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

        [SerializeField] private SequenceMode sequenceMode = SequenceMode.RandomNoImmediateRepeat;
        private int nextClipToPlay = -1;
        private int lastClipPlayed = -1;

        private void OnEnable()
        {
            hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        public override async UniTask<ClipData> GetClipData()
        {
            var data = ClipDataPool.Pop();
            var reference = GetNextClip();
            data.clips.Add(await reference.LoadAsync());
            return data;
        }

        private AudioClipReference GetNextClip()
        {
            if (references.Length == 1) return references[0];

            if (nextClipToPlay == -1)
            {
                nextClipToPlay = sequenceMode == SequenceMode.Sequential ? 0 : Random.Range(0, references.Length);
            }
            else
            {
                switch (sequenceMode)
                {
                    default:
                    case SequenceMode.Random:
                        nextClipToPlay = Random.Range(0, references.Length);
                        break;

                    case SequenceMode.RandomNoImmediateRepeat:
                        do
                        {
                            nextClipToPlay = Random.Range(0, references.Length);
                        } while (nextClipToPlay == lastClipPlayed);

                        break;

                    case SequenceMode.Sequential:
                        nextClipToPlay = (int)Mathf.Repeat(++nextClipToPlay, references.Length);
                        break;
                }
            }

            lastClipPlayed = nextClipToPlay;

            return references[nextClipToPlay];
        }
    }
}
#endif