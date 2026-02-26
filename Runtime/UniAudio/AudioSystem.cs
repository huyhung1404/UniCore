#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UniCore.Audio.Node;
using UniCore.Audio.Pool;
using UniCore.Signal;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        internal static AudioSystem s_Instance;
        internal AudioSettings Settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (s_Instance != null) return;
            var go = new GameObject("AudioSystem");
            go.AddComponent<AudioSystem>();
            go.AddComponent<AudioListener>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            s_Instance = this;
            Settings = Resources.Load<AudioSettings>(nameof(AudioSettings));
            SoundEmitterPool.Prewarm(Settings.PoolInitialSize);

            m_masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1);
            m_musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1);
            m_sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1);

            SetGroupVolume("MasterVolume", m_masterVolume);
            SetGroupVolume("MusicVolume", m_musicVolume);
            SetGroupVolume("SFXVolume", m_sfxVolume);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
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
            var volumeSet = Settings.OutputMixer.SetFloat(parameterName, NormalizedToMixerValue(normalizedVolume));
            if (!volumeSet) Debug.LogError("The AudioMixer parameter was not found");
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
            var (config, clipData) = await UniTask.WhenAll(GetConfiguration(signal.Config), GetClipData(signal.Clip));
            foreach (var clip in clipData.Clips)
            {
                var soundEmitter = SoundEmitterPool.Pop();
                soundEmitter.PlayAudioClip(signal, clip, config);
            }

            ClipDataPool.Push(clipData);
        }

        public static async UniTask<AudioConfiguration> GetConfiguration(string config)
        {
            var handle = Addressables.LoadAssetAsync<AudioConfiguration>($"{s_Instance.Settings.GroupAddress}/Configs/{config}.asset");
            if (handle.IsDone && handle.IsValid()) return handle.Result;
            return await handle.ToUniTask();
        }

        public static async UniTask<ClipData> GetClipData(string clip)
        {
            var node = await GetClipNode(clip);
            return await node.GetClipData();
        }

        public static async UniTask<BaseAudioNode> GetClipNode(string clip)
        {
            var handle = Addressables.LoadAssetAsync<BaseAudioNode>($"{s_Instance.Settings.GroupAddress}/Nodes/{clip}.asset");
            if (handle.IsDone && handle.IsValid()) return handle.Result;
            return await handle.ToUniTask();
        }
    }
}
#endif