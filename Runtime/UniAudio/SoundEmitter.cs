#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UniCore.Signal;
using UnityEngine;

namespace UniCore.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour, ISignalListener<StopSoundSignal>, ISignalListener<ChangeSoundSignal>
    {
        private AudioSource _source;
        private int? _configHash;
        private float _timeRequire;
        private bool _needReturnToPool;
        private int? _soundId;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        public void PlayAudioClip(in PlaySoundSignal signal, AudioClip clip, AudioConfiguration configuration)
        {
            _source.clip = clip;
            if (configuration != null && (_configHash == null || _configHash != configuration.GetHashCode()))
            {
                _configHash = configuration.GetHashCode();
                UpdateConfig(configuration);
            }

            transform.parent = signal.Parent;
            transform.position = signal.Position;

            _source.loop = signal.IsLoop;
            _source.time = 0f;
            _source.Play();
            _timeRequire = clip.length / Mathf.Abs(_source.pitch);
            if (signal.IsLoop) _timeRequire = 0f;
            _needReturnToPool = true;
            _soundId = signal.SoundId;
            if (_soundId != null) RegisterSoundEvent();
        }

        private void RegisterSoundEvent()
        {
            SignalSystem.Register<StopSoundSignal>(this);
            SignalSystem.Register<ChangeSoundSignal>(this);
        }

        private void Stop()
        {
            _timeRequire = 0;
            _source.Stop();
            NotifyBeingFinish();
        }

        public bool IsPlaying()
        {
            return _source.isPlaying && _timeRequire > 0;
        }

        private void NotifyBeingFinish()
        {
            if (!_needReturnToPool) return;
            SoundEmitterPool.Push(this);
            _needReturnToPool = false;
        }

        private void LateUpdate()
        {
            if (_timeRequire > 0)
            {
                _timeRequire -= Time.deltaTime;
                return;
            }

            NotifyBeingFinish();
        }

        public void OnSignal(StopSoundSignal signal)
        {
            if (signal.SoundId == _soundId) Stop();
        }

        public void OnSignal(ChangeSoundSignal signal)
        {
            if (signal.SoundId != _soundId) return;
            _ = ChangeSound(signal.Clip);
        }

        private async UniTaskVoid ChangeSound(string clipAddress)
        {
            if (!_source) return;
            var currentSoundId = _soundId;
            var clipData = await AudioSystem.GetClipData(clipAddress);
            if (clipData == null || currentSoundId != _soundId) return;
            var newClip = clipData.Clips[0];
            const float fadeOutTime = 0.15f;
            var startVolume = _source.volume;
            var t = 0f;

            while (t < fadeOutTime && _source && _source.isPlaying)
            {
                t += Time.deltaTime;
                _source.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutTime);
                await UniTask.Yield();
            }

            _source.Stop();

            _source.clip = newClip;
            _source.time = 0f;
            _source.Play();

            if (!_source.loop) _timeRequire = newClip.length / Mathf.Abs(_source.pitch);

            const float fadeInTime = 0.15f;
            t = 0f;

            while (t < fadeInTime && _source)
            {
                t += Time.deltaTime;
                _source.volume = Mathf.Lerp(0f, startVolume, t / fadeInTime);
                await UniTask.Yield();
            }

            _source.volume = startVolume;
        }

        private void OnDisable()
        {
            if (_soundId != null)
            {
                SignalSystem.Unregister<StopSoundSignal>(this);
                SignalSystem.Unregister<ChangeSoundSignal>(this);
                SignalSystem.Dispatch(new SoundFinishSignal
                {
                    SoundId = _soundId.Value
                });
            }

            if (!IsPlaying()) return;
            Stop();
        }

        private void UpdateConfig(AudioConfiguration configuration)
        {
            _source.outputAudioMixerGroup = configuration.Output;
            _source.mute = configuration.Mute;
            _source.bypassEffects = configuration.BypassEffects;
            _source.bypassListenerEffects = configuration.BypassListenerEffects;
            _source.bypassReverbZones = configuration.BypassReverbZones;

            _source.priority = configuration.Priority;
            _source.volume = configuration.Volume;
            _source.pitch = configuration.Pitch;
            _source.panStereo = configuration.StereoPan;
            _source.spatialBlend = configuration.SpatialBlend;
            _source.reverbZoneMix = configuration.ReverbZoneMix;

            _source.dopplerLevel = configuration.DopplerLevel;
            _source.spread = configuration.Spread;
            _source.rolloffMode = configuration.VolumeRolloff;
            _source.minDistance = configuration.MinDistance;
            _source.maxDistance = configuration.MaxDistance;
        }
    }
}
#endif