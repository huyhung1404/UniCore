using UnityEngine;
using UnityEngine.Audio;

namespace UniCore.Audio
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Configuration")]
    public class AudioConfiguration : ScriptableObject
    {
        public AudioMixerGroup Output;
        public bool Mute;
        public bool BypassEffects;
        public bool BypassListenerEffects;
        public bool BypassReverbZones;

        public int Priority;
        public float Volume = 1f;
        public float Pitch = 1f;
        public float StereoPan;
        public float SpatialBlend = 1f;
        public float ReverbZoneMix = 1f;

        public float DopplerLevel = 1f;
        public int Spread;
        public AudioRolloffMode VolumeRolloff = AudioRolloffMode.Logarithmic;
        public float MinDistance = 0.1f;
        public float MaxDistance = 50f;
    }
}