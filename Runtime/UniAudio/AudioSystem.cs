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
        [Header("Audio control")] [Range(0f, 1f), SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f), SerializeField] private float musicVolume = 1f;
        [Range(0f, 1f), SerializeField] private float sfxVolume = 1f;
        internal static AudioSystem instance;
        internal AudioSettings settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Create()
        {
            if (instance != null) return;
            var go = new GameObject("AudioSystem");
            go.AddComponent<AudioSystem>();
            go.AddComponent<AudioListener>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            instance = this;
            settings = Resources.Load<AudioSettings>(nameof(AudioSettings));
            SoundEmitterPool.Prewarm(settings.PoolInitialSize);

            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1);

            SetGroupVolume("MasterVolume", masterVolume);
            SetGroupVolume("MusicVolume", musicVolume);
            SetGroupVolume("SFXVolume", sfxVolume);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            SetGroupVolume("MasterVolume", masterVolume);
            SetGroupVolume("MusicVolume", musicVolume);
            SetGroupVolume("SFXVolume", sfxVolume);
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
            masterVolume = signal.volume;
            SetGroupVolume("MasterVolume", masterVolume);
        }

        public void OnSignal(ChangeMusicVolumeSignal signal)
        {
            musicVolume = signal.volume;
            SetGroupVolume("MusicVolume", musicVolume);
        }

        public void OnSignal(ChangeSFXVolumeSignal signal)
        {
            sfxVolume = signal.volume;
            SetGroupVolume("SFXVolume", sfxVolume);
        }

        private void SetGroupVolume(string parameterName, float normalizedVolume)
        {
            var volumeSet = settings.OutputMixer.SetFloat(parameterName, NormalizedToMixerValue(normalizedVolume));
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
            var (config, clipData) = await UniTask.WhenAll(GetConfiguration(signal.config), GetClipData(signal.clip));
            foreach (var clip in clipData.clips)
            {
                var soundEmitter = SoundEmitterPool.Pop();
                soundEmitter.PlayAudioClip(signal, clip, config);
            }

            ClipDataPool.Push(clipData);
        }

        public static async UniTask<AudioConfiguration> GetConfiguration(string config)
        {
            var handle = Addressables.LoadAssetAsync<AudioConfiguration>($"{instance.settings.GroupAddress}/Configs/{config}.asset");
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
            var handle = Addressables.LoadAssetAsync<BaseAudioNode>($"{instance.settings.GroupAddress}/Nodes/{clip}.asset");
            if (handle.IsDone && handle.IsValid()) return handle.Result;
            return await handle.ToUniTask();
        }
    }
}
#endif