#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UniCore.Audio.Node
{
    [Serializable]
    public class ScattererAudioNode : BaseAudioNode
    {
        [SerializeField] private AudioClipReference[] m_references = Array.Empty<AudioClipReference>();

        [Header("Scatter Settings")] [SerializeField] private float m_minDelay = 1f;
        [SerializeField] private float m_maxDelay = 5f;
        [SerializeField] private float m_spawnRadius = 15f;

        public override async UniTask<ClipData> GetClipData(PlaySoundSignal signal)
        {
            if (signal.SoundId.HasValue)
            {
                await RunScattererLoop(signal);
            }
            else
            {
                Debug.LogWarning("[UniAudio] ScattererNode yêu cầu PlaySoundSignal phải có SoundId để quản lý vòng đời!");
            }

            return null;
        }

        private async UniTask RunScattererLoop(PlaySoundSignal signal)
        {
            while (AudioSystem.s_Instance != null && AudioSystem.s_Instance.IsSoundActive(signal.SoundId))
            {
                var waitTime = Random.Range(m_minDelay, m_maxDelay);
                await UniTask.WaitForSeconds(waitTime);

                if (AudioSystem.s_Instance == null || !AudioSystem.s_Instance.IsSoundActive(signal.SoundId)) break;

                var clipRef = m_references[Random.Range(0, m_references.Length)];
                var clip = await clipRef.LoadAsync();

                if (clip != null && AudioSystem.s_Instance.IsSoundActive(signal.SoundId))
                {
                    var emitter = SoundEmitterPool.Pop();
                    var config = AudioSystem.s_Instance.SearchSystem.GetConfiguration(signal.ConfigId.AsSpan());

                    var randomOffset = Random.insideUnitSphere * m_spawnRadius;

                    var childSignal = signal;
                    childSignal.Position += randomOffset;
                    childSignal.IsLoop = false;
                    childSignal.SoundId = null;

                    var cmd = new ClipPlayCommand
                    {
                        Clip = clip, Delay = 0f, Reference = clipRef, ReleaseDelay = ReleaseDelay, LayerIndex = 0, LayerVolume = 1f
                    };

                    emitter.PlayAudioCommand(childSignal, cmd, config);
                }
            }
        }

        public override (Log, string) IsValid()
        {
            if (string.IsNullOrEmpty(NodeName)) return (Log.Error, "Node name is empty.");
            if (m_references == null || m_references.Length == 0) return (Log.Warning, "References empty.");
            return (Log.None, null);
        }
    }
}
#endif