#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UniCore.Signal;
using UnityEngine;

namespace UniCore.Audio
{
    public class AudioSystem : MonoBehaviour,
        ISignalListener<ChangeMasterVolumeSignal>,
        ISignalListener<ChangeMusicVolumeSignal>,
        ISignalListener<ChangeSFXVolumeSignal>,
        ISignalListener<PlaySoundSignal>,
        ISignalListener<StopSoundSignal>,
        ISignalListener<SoundFinishSignal>,
        ISignalListener<ChangeSnapshotSignal> // [MỚI]
    {
        [Header("Audio control")] 
        [Range(0f, 1f), SerializeField] private float m_masterVolume = 1f;
        [Range(0f, 1f), SerializeField] private float m_musicVolume = 1f;
        [Range(0f, 1f), SerializeField] private float m_sfxVolume = 1f;
        
        internal static AudioSystem s_Instance;
        internal AudioRuntimeSettings RuntimeSettings;
        internal AudioSearchSystem SearchSystem;

        private readonly HashSet<int> _activeSounds = new HashSet<int>();
        private readonly Dictionary<int, int> _nodePlayCounts = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _soundIdToNodeHash = new Dictionary<int, int>();
        
        private int _duckingCount;

#if UNITY_EDITOR
        public static int TotalCulledCount { get; private set; }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (s_Instance != null) return;
            var go = new GameObject("AudioSystem");
            s_Instance = go.AddComponent<AudioSystem>();
            go.AddComponent<AudioListener>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;

#if UNITY_EDITOR
            RuntimeSettings = AudioEditorSettings.CreateRuntimeInstance();
            TotalCulledCount = 0;
#else
            RuntimeSettings = Resources.Load<AudioRuntimeSettings>(AudioRuntimeSettings.k_FileName);
#endif

            if (RuntimeSettings == null)
            {
                Debug.LogError("[UniAudio] AudioRuntimeSettings is missing.");
                return;
            }
            
            SearchSystem = new AudioSearchSystem().WithRuntimeSettings(RuntimeSettings);

            SoundEmitterPool.Prewarm(RuntimeSettings.PoolInitialSize);

            m_masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1);
            m_musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1);
            m_sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1);

            SetGroupVolume("MasterVolume", m_masterVolume);
            SetGroupVolume("MusicVolume", m_musicVolume);
            SetGroupVolume("SFXVolume", m_sfxVolume);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying || RuntimeSettings == null) return;
            SetGroupVolume("MasterVolume", m_masterVolume);
            SetGroupVolume("MusicVolume", m_musicVolume);
            SetGroupVolume("SFXVolume", m_sfxVolume);
        }

        private void OnEnable()
        {
            SignalSystem.Register<ChangeMasterVolumeSignal>(this);
            SignalSystem.Register<ChangeMusicVolumeSignal>(this);
            SignalSystem.Register<ChangeSFXVolumeSignal>(this);
            SignalSystem.Register<PlaySoundSignal>(this);
            SignalSystem.Register<StopSoundSignal>(this);
            SignalSystem.Register<SoundFinishSignal>(this);
            SignalSystem.Register<ChangeSnapshotSignal>(this);
        }

        private void OnDisable()
        {
            SignalSystem.Unregister<ChangeMasterVolumeSignal>(this);
            SignalSystem.Unregister<ChangeMusicVolumeSignal>(this);
            SignalSystem.Unregister<ChangeSFXVolumeSignal>(this);
            SignalSystem.Unregister<PlaySoundSignal>(this);
            SignalSystem.Unregister<StopSoundSignal>(this);
            SignalSystem.Unregister<SoundFinishSignal>(this);
            SignalSystem.Unregister<ChangeSnapshotSignal>(this);
        }

        #region Volume, Ducking & Snapshot
        
        public void OnSignal(ChangeSnapshotSignal signal)
        {
            if (RuntimeSettings == null || RuntimeSettings.OutputMixer == null) return;
            
            var snapshot = RuntimeSettings.OutputMixer.FindSnapshot(signal.SnapshotName);
            if (snapshot != null)
            {
                snapshot.TransitionTo(signal.TransitionTime);
            }
            else
            {
                Debug.LogWarning($"[UniAudio] Snapshot '{signal.SnapshotName}' không tồn tại trong Mixer!");
            }
        }

        public void OnSignal(ChangeMasterVolumeSignal signal)
        {
            m_masterVolume = signal.Volume;
            SetGroupVolume("MasterVolume", m_masterVolume);
        }

        public void OnSignal(ChangeMusicVolumeSignal signal)
        {
            m_musicVolume = signal.Volume;
            SetGroupVolume("MusicVolume", m_musicVolume);
        }

        public void OnSignal(ChangeSFXVolumeSignal signal)
        {
            m_sfxVolume = signal.Volume;
            SetGroupVolume("SFXVolume", m_sfxVolume);
        }

        private void SetGroupVolume(string parameterName, float normalizedVolume)
        {
            if (RuntimeSettings == null || RuntimeSettings.OutputMixer == null) return;
            var volumeSet = RuntimeSettings.OutputMixer.SetFloat(parameterName, NormalizedToMixerValue(normalizedVolume));
            if (!volumeSet) Debug.LogWarning($"[UniAudio] AudioMixer parameter '{parameterName}' not found.");
        }

        private static float NormalizedToMixerValue(float normalizedValue)
        {
            return (normalizedValue - 1f) * 80f;
        }

        private async UniTaskVoid ApplyDuckingAsync(float ratio, float fadeTime, int? soundId)
        {
            _duckingCount++;
            var targetVolume = m_musicVolume * ratio;
            var t = 0f;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                SetGroupVolume("MusicVolume", Mathf.Lerp(m_musicVolume, targetVolume, t / fadeTime));
                await UniTask.Yield();
            }

            if (soundId.HasValue)
            {
                await UniTask.WaitUntil(() => !_activeSounds.Contains(soundId.Value));
            }

            _duckingCount--;
            if (_duckingCount > 0) return;

            t = 0f;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                SetGroupVolume("MusicVolume", Mathf.Lerp(targetVolume, m_musicVolume, t / fadeTime));
                await UniTask.Yield();
            }
            SetGroupVolume("MusicVolume", m_musicVolume);
        }

        #endregion

        #region Sound State & Culling Management

        public void OnSignal(PlaySoundSignal signal)
        {
            if (string.IsNullOrEmpty(signal.NodePath)) return;
            
            var nodeHash = AudioSearchSystem.CalculateHash(signal.NodePath.AsSpan());
            var node = SearchSystem?.FindNode(signal.NodePath.AsSpan());

            if (node != null && node.MaxInstances > 0)
            {
                _nodePlayCounts.TryGetValue(nodeHash, out var currentCount);
                if (currentCount >= node.MaxInstances)
                {
#if UNITY_EDITOR
                    TotalCulledCount++;
#endif
                    return;
                }

                _nodePlayCounts[nodeHash] = currentCount + 1;
            }

            if (signal.SoundId.HasValue)
            {
                _activeSounds.Add(signal.SoundId.Value);
                _soundIdToNodeHash[signal.SoundId.Value] = nodeHash;
            }

            _ = PlaySound(signal);
        }

        private void FreeNodeCount(int soundId)
        {
            _activeSounds.Remove(soundId);
            if (_soundIdToNodeHash.TryGetValue(soundId, out var hash))
            {
                if (_nodePlayCounts.TryGetValue(hash, out var count))
                {
                    _nodePlayCounts[hash] = Mathf.Max(0, count - 1);
                }
                _soundIdToNodeHash.Remove(soundId);
            }
        }

        public void OnSignal(StopSoundSignal signal) => FreeNodeCount(signal.SoundId);
        public void OnSignal(SoundFinishSignal signal) => FreeNodeCount(signal.SoundId);

        #endregion

        private static async UniTaskVoid PlaySound(PlaySoundSignal signal)
        {
            if (s_Instance == null || s_Instance.SearchSystem == null) return;

            var config = s_Instance.SearchSystem.GetConfiguration(signal.ConfigId.AsSpan());
            var node = s_Instance.SearchSystem.FindNode(signal.NodePath.AsSpan());

            if (node == null) return;

            if (config != null && config.IsDucking)
            {
                s_Instance.ApplyDuckingAsync(config.DuckingRatio, config.DuckingFadeTime, signal.SoundId).Forget();
            }

            var clipData = await node.GetClipData(signal);

            if (signal.SoundId.HasValue && !s_Instance._activeSounds.Contains(signal.SoundId.Value))
            {
                if (clipData != null)
                {
                    foreach (var cmd in clipData.Commands) cmd.Reference?.ReleaseUsage(cmd.ReleaseDelay);
                    ClipDataPool.Push(clipData);
                }
                return;
            }

            if (clipData == null || clipData.Commands == null || clipData.Commands.Count == 0) return;

            foreach (var command in clipData.Commands)
            {
                var soundEmitter = SoundEmitterPool.Pop();
                soundEmitter.PlayAudioCommand(signal, command, config);
            }

            ClipDataPool.Push(clipData);
        }
        
        public bool IsSoundActive(int? soundId)
        {
            if (!soundId.HasValue) return false;
            return _activeSounds.Contains(soundId.Value);
        }
    }
}
#endif