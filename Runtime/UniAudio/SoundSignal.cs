using UniCore.Signal;
using UnityEngine;

namespace UniCore.Audio
{
    public struct ChangeMasterVolumeSignal : ISignalEvent
    {
        public float volume;
    }

    public struct ChangeMusicVolumeSignal : ISignalEvent
    {
        public float volume;
    }

    public struct ChangeSFXVolumeSignal : ISignalEvent
    {
        public float volume;
    }

    public struct PlaySoundSignal : ISignalEvent
    {
        public string clip;
        public string config;

        public bool isLoop;

        public Vector3 position;
        public Transform parent;

        public int? soundId;
    }

    public struct StopSoundSignal : ISignalEvent
    {
        public int soundId;
    }

    public struct SoundFinishSignal : ISignalEvent
    {
        public int soundId;
    }
}