#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniCore.Audio.Pool;
using UniCore.Signal;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UniCore.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour, 
        ISignalListener<StopSoundSignal>, 
        ISignalListener<ChangeSoundSignal>,
        ISignalListener<ChangeLayerVolumeSignal>
    {
        private AudioSource _source;
        private int? _configHash;
        private float _timeRequire;
        private bool _needReturnToPool;
        private int? _soundId;
        
        private ClipPlayCommand _currentCommand;
        private float? _overrideReleaseDelay;
        
        private float _baseConfigVolume;
        private float _layerVolumeMultiplier;

#if UNITY_EDITOR
        public static readonly HashSet<SoundEmitter> ActiveEmitters = new HashSet<SoundEmitter>();
        public float Progress => _source && _source.clip ? _source.time / _source.clip.length : 0f;
        public string ClipName => _source && _source.clip ? _source.clip.name : "Loading...";
        public float CurrentVolume => _source ? _source.volume : 0f;
        public bool IsLooping => _source && _source.loop;
#endif

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        public void PlayAudioCommand(in PlaySoundSignal signal, ClipPlayCommand command, AudioConfiguration configuration)
        {
            _currentCommand = command;
            _overrideReleaseDelay = signal.OverrideReleaseDelay;
            _source.clip = command.Clip;
            
            if (configuration != null && (_configHash == null || _configHash != configuration.GetHashCode()))
            {
                _configHash = configuration.GetHashCode();
                UpdateConfig(configuration);
            }

            var baseVol = configuration?.Volume ?? 1f;
            var volVar = configuration?.VolumeVariance ?? 0f;
            var basePitch = configuration?.Pitch ?? 1f;
            var pitchVar = configuration?.PitchVariance ?? 0f;

            _baseConfigVolume = Mathf.Clamp01(baseVol + Random.Range(-volVar, volVar));
            _layerVolumeMultiplier = command.LayerVolume;

            _source.volume = _baseConfigVolume * _layerVolumeMultiplier;
            _source.pitch = Mathf.Clamp(basePitch + Random.Range(-pitchVar, pitchVar), -3f, 3f);

            transform.SetParent(signal.Parent, false);
            transform.position = signal.Position;

            _source.loop = signal.IsLoop;
            _soundId = signal.SoundId;

            if (command.Delay > 0f)
            {
                PlayWithDelay(command.Delay, signal.IsLoop).Forget();
            }
            else
            {
                StartPlaying(signal.IsLoop, command.Clip);
            }

            if (_soundId != null) RegisterSoundEvent();
        }

        private async UniTaskVoid PlayWithDelay(float delay, bool isLoop)
        {
            await UniTask.WaitForSeconds(delay);
            if (!_source || _soundId == null)
            {
                var releaseTime = _overrideReleaseDelay ?? _currentCommand.ReleaseDelay;
                _currentCommand.Reference?.ReleaseUsage(releaseTime);
                return;
            }
            StartPlaying(isLoop, _currentCommand.Clip);
        }

        private void StartPlaying(bool isLoop, AudioClip clip)
        {
            _source.time = 0f;
            _source.Play();
            _timeRequire = isLoop ? float.PositiveInfinity : (clip.length / Mathf.Abs(_source.pitch));
            _needReturnToPool = true;

#if UNITY_EDITOR
            ActiveEmitters.Add(this);
#endif
        }

        private void Stop()
        {
            _timeRequire = 0f;
            _source.Stop();
            
            var delay = _overrideReleaseDelay ?? _currentCommand.ReleaseDelay;
            _currentCommand.Reference?.ReleaseUsage(delay);
            
            NotifyBeingFinish();
        }

        public bool IsPlaying()
        {
            return _source.isPlaying && _timeRequire > 0f;
        }

        private void NotifyBeingFinish()
        {
#if UNITY_EDITOR
            ActiveEmitters.Remove(this);
#endif
            if (!_needReturnToPool) return;
            SoundEmitterPool.Push(this);
            _needReturnToPool = false;
        }

        private void LateUpdate()
        {
            if (_timeRequire > 0f)
            {
                if (!float.IsPositiveInfinity(_timeRequire))
                {
                    _timeRequire -= Time.deltaTime;
                }
                return;
            }

            NotifyBeingFinish();
        }

        public void OnSignal(ChangeLayerVolumeSignal signal)
        {
            if (signal.SoundId != _soundId || signal.LayerIndex != _currentCommand.LayerIndex) return;
            ChangeLayerVolumeAsync(signal.TargetVolume, signal.FadeTime).Forget();
        }

        private async UniTaskVoid ChangeLayerVolumeAsync(float targetVolume, float fadeTime)
        {
            if (!_source) return;
            var currentSoundId = _soundId;
            var startVol = _layerVolumeMultiplier;
            var t = 0f;
            
            while (t < fadeTime && _source && _source.isPlaying)
            {
                if (currentSoundId != _soundId) return;
                t += Time.deltaTime;
                _layerVolumeMultiplier = Mathf.Lerp(startVol, targetVolume, t / fadeTime);
                _source.volume = _baseConfigVolume * _layerVolumeMultiplier;
                await UniTask.Yield();
            }

            if (currentSoundId == _soundId)
            {
                _layerVolumeMultiplier = targetVolume;
                _source.volume = _baseConfigVolume * _layerVolumeMultiplier;
            }
        }

        public void OnSignal(StopSoundSignal signal)
        {
            if (signal.SoundId == _soundId) Stop();
        }

        public void OnSignal(ChangeSoundSignal signal)
        {
            if (signal.SoundId != _soundId) return;
            _ = ChangeSound(signal.NodePath);
        }

        private async UniTaskVoid ChangeSound(string clipAddress)
        {
            if (!_source || AudioSystem.s_Instance == null || AudioSystem.s_Instance.SearchSystem == null) return;
            
            var currentSoundId = _soundId;
            var node = AudioSystem.s_Instance.SearchSystem.FindNode(clipAddress.AsSpan());
            if (node == null) return;
            
            var tempSignal = new PlaySoundSignal
            {
                SoundId = currentSoundId,
                NodePath = clipAddress
            };

            var clipData = await node.GetClipData(tempSignal);
            
            if (clipData == null || currentSoundId != _soundId)
            {
                if (clipData != null) 
                {
                    foreach (var cmd in clipData.Commands) cmd.Reference?.ReleaseUsage(cmd.ReleaseDelay);
                    ClipDataPool.Push(clipData);
                }
                return;
            }

            var newCommand = clipData.Commands[0];
            ClipDataPool.Push(clipData);

            const float fadeOutTime = 0.15f;
            var startVolume = _source.volume;
            var t = 0f;
            
            while (t < fadeOutTime && _source && _source.isPlaying)
            {
                if (currentSoundId != _soundId)
                {
                    newCommand.Reference?.ReleaseUsage(newCommand.ReleaseDelay);
                    return;
                }

                t += Time.deltaTime;
                _source.volume = Mathf.Lerp(startVolume, 0f, t / fadeOutTime);
                await UniTask.Yield();
            }

            if (currentSoundId != _soundId) 
            {
                newCommand.Reference?.ReleaseUsage(newCommand.ReleaseDelay);
                return;
            }

            _source.Stop();
            
            var delay = _overrideReleaseDelay ?? _currentCommand.ReleaseDelay;
            _currentCommand.Reference?.ReleaseUsage(delay);
            
            _currentCommand = newCommand;
            _layerVolumeMultiplier = newCommand.LayerVolume;
            var targetVolume = _baseConfigVolume * _layerVolumeMultiplier;
            
            _source.clip = newCommand.Clip;
            _source.time = 0f;
            _source.Play();

            _timeRequire = _source.loop ? float.PositiveInfinity : (newCommand.Clip.length / Mathf.Abs(_source.pitch));

            const float fadeInTime = 0.15f;
            t = 0f;
            
            while (t < fadeInTime && _source)
            {
                if (currentSoundId != _soundId) return;

                t += Time.deltaTime;
                _source.volume = Mathf.Lerp(0f, targetVolume, t / fadeInTime);
                await UniTask.Yield();
            }

            if (currentSoundId == _soundId) _source.volume = targetVolume;
        }

        private void RegisterSoundEvent()
        {
            SignalSystem.Register<StopSoundSignal>(this);
            SignalSystem.Register<ChangeSoundSignal>(this);
            SignalSystem.Register<ChangeLayerVolumeSignal>(this);
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            ActiveEmitters.Remove(this);
#endif
            if (_soundId != null)
            {
                SignalSystem.Unregister<StopSoundSignal>(this);
                SignalSystem.Unregister<ChangeSoundSignal>(this);
                SignalSystem.Unregister<ChangeLayerVolumeSignal>(this);
                
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