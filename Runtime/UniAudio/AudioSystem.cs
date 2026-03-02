#if HAS_UNITASK && HAS_ADDRESSABLES
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
        ISignalListener<PlaySoundSignal>
    {
        [Header("Audio control")] [Range(0f, 1f), SerializeField] private float m_masterVolume = 1f;
        [Range(0f, 1f), SerializeField] private float m_musicVolume = 1f;
        [Range(0f, 1f), SerializeField] private float m_sfxVolume = 1f;
        
        public static AudioSystem s_Instance;

        public AudioRuntimeSettings RuntimeSettings;
        public AudioSearchSystem SearchSystem;

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
#else
            RuntimeSettings = Resources.Load<AudioRuntimeSettings>(AudioRuntimeSettings.k_FileName);
#endif

            if (RuntimeSettings == null)
            {
                Debug.LogError("[UniAudio] AudioRuntimeSettings is not exits.");
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
        }

        private void OnDisable()
        {
            SignalSystem.Unregister<ChangeMasterVolumeSignal>(this);
            SignalSystem.Unregister<ChangeMusicVolumeSignal>(this);
            SignalSystem.Unregister<ChangeSFXVolumeSignal>(this);
            SignalSystem.Unregister<PlaySoundSignal>(this);
        }

        #region Volume

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
            if (!volumeSet) Debug.LogWarning($"[UniAudio] AudioMixer parameter: {parameterName} is not exits.");
        }

        private static float NormalizedToMixerValue(float normalizedValue)
        {
            return (normalizedValue - 1f) * 80f;
        }

        #endregion

        public void OnSignal(PlaySoundSignal signal)
        {
            _ = PlaySound(signal);
        }

        private static async UniTaskVoid PlaySound(PlaySoundSignal signal)
        {
            if (s_Instance == null || s_Instance.SearchSystem == null) return;

            var config = s_Instance.SearchSystem.GetConfiguration(signal.ConfigId);
            var node = s_Instance.SearchSystem.FindNode(signal.NodePath);

            if (node == null)
            {
                Debug.LogWarning($"[UniAudio] Node: {signal.NodePath} is not exits");
                return;
            }

            var clipData = await node.GetClipData();

            if (clipData == null || clipData.Clips == null || clipData.Clips.Count == 0) return;

            foreach (var clip in clipData.Clips)
            {
                if (clip == null) continue;
                var soundEmitter = SoundEmitterPool.Pop();
                soundEmitter.PlayAudioClip(signal, clip, config);
            }

            ClipDataPool.Push(clipData);
        }
    }
}
#endif