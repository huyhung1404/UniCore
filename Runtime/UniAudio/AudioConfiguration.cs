using UnityEngine;
using UnityEngine.Audio;

namespace UniCore.Audio
{
    [CreateAssetMenu(menuName = "UniCore/Audio/Configuration")]
    public class AudioConfiguration : ScriptableObject
    {
        public AudioMixerGroup output;
        public bool mute;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;

        public int priority;
        public float volume = 1f;
        public float pitch = 1f;
        public float stereoPan;
        public float spatialBlend = 1f;
        public float reverbZoneMix = 1f;

        public float dopplerLevel = 1f;
        public int spread;
        public AudioRolloffMode volumeRolloff = AudioRolloffMode.Logarithmic;
        public float minDistance = 0.1f;
        public float maxDistance = 50f;
    }
}