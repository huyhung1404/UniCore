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
        private AudioSource source;
        private int configHash = -1;
        private float timeRequire;
        private bool needReturnToPool;
        private int? soundId;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void PlayAudioClip(in PlaySoundSignal signal, AudioClip clip, AudioConfiguration configuration)
        {
            source.clip = clip;
            if (configuration != null && configHash != configuration.GetHashCode())
            {
                configHash = configuration.GetHashCode();
                UpdateConfig(configuration);
            }

            transform.parent = signal.parent;
            transform.position = signal.position;

            source.loop = signal.isLoop;
            source.time = 0f;
            source.Play();
            timeRequire = clip.length / Mathf.Abs(source.pitch);
            if (signal.isLoop) timeRequire = 0f;
            needReturnToPool = true;
            soundId = signal.soundId;
            if (soundId != null) RegisterSoundEvent();
        }

        private void RegisterSoundEvent()
        {
            SignalSystem.Register<StopSoundSignal>(this);
            SignalSystem.Register<ChangeSoundSignal>(this);
        }

        private void Stop()
        {
            timeRequire = 0;
            if (IsPlaying()) source.Stop();
            NotifyBeingFinish();
        }

        public bool IsPlaying()
        {
            return source.isPlaying;
        }

        private void NotifyBeingFinish()
        {
            if (!needReturnToPool) return;
            SoundEmitterPool.Push(this);
            needReturnToPool = false;
        }

        private void LateUpdate()
        {
            if (timeRequire > 0)
            {
                timeRequire -= Time.deltaTime;
                return;
            }

            NotifyBeingFinish();
        }

        public void OnSignal(StopSoundSignal signal)
        {
            if (signal.soundId == soundId) Stop();
        }
        
        public void OnSignal(ChangeSoundSignal signal)
        {
            if (signal.soundId != soundId) return;
            _ = ChangeSound(signal.clip);
        }

        private async UniTaskVoid ChangeSound(string clipAddress)
        {
            if (!source) return;
            var currentSoundId = soundId;
            var clipData = await AudioSystem.GetClipData(clipAddress);
            if (clipData == null || currentSoundId != soundId) return;
            var newClip = clipData.clips[0];
            const float fadeOutTime = 0.15f;
            var startVolume = source.volume;
            var t = 0f;

            while (t < fadeOutTime && source && source.isPlaying)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutTime);
                await UniTask.Yield();
            }

            source.Stop();
            
            source.clip = newClip;
            source.time = 0f;
            source.Play();
            
            if (!source.loop) timeRequire = newClip.length / Mathf.Abs(source.pitch);
            
            const float fadeInTime = 0.15f;
            t = 0f;

            while (t < fadeInTime && source)
            {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, startVolume, t / fadeInTime);
                await UniTask.Yield();
            }

            source.volume = startVolume;
        }

        private void OnDisable()
        {
            if (soundId != null)
            {
                SignalSystem.Unregister<StopSoundSignal>(this);
                SignalSystem.Unregister<ChangeSoundSignal>(this);
                SignalSystem.Dispatch(new SoundFinishSignal
                {
                    soundId = soundId.Value
                });
            }

            Stop();
        }

        private void UpdateConfig(AudioConfiguration configuration)
        {
            source.outputAudioMixerGroup = configuration.output;
            source.mute = configuration.mute;
            source.bypassEffects = configuration.bypassEffects;
            source.bypassListenerEffects = configuration.bypassListenerEffects;
            source.bypassReverbZones = configuration.bypassReverbZones;

            source.priority = configuration.priority;
            source.volume = configuration.volume;
            source.pitch = configuration.pitch;
            source.panStereo = configuration.stereoPan;
            source.spatialBlend = configuration.spatialBlend;
            source.reverbZoneMix = configuration.reverbZoneMix;

            source.dopplerLevel = configuration.dopplerLevel;
            source.spread = configuration.spread;
            source.rolloffMode = configuration.volumeRolloff;
            source.minDistance = configuration.minDistance;
            source.maxDistance = configuration.maxDistance;
        }
    }
}
#endif