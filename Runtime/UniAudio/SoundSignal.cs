using UniCore.Signal;
using UnityEngine;

namespace UniCore.Audio
{
    public struct ChangeMasterVolumeSignal : ISignalEvent
    {
        public float Volume;
    }

    public struct ChangeMusicVolumeSignal : ISignalEvent
    {
        public float Volume;
    }

    public struct ChangeSFXVolumeSignal : ISignalEvent
    {
        public float Volume;
    }

    public struct PlaySoundSignal : ISignalEvent
    {
        public string NodePath;
        public string ConfigId;

        public bool IsLoop;

        public Vector3 Position;
        public Transform Parent;

        public int? SoundId;
        public float? OverrideReleaseDelay;
    }
    
    public struct ChangeSnapshotSignal : ISignalEvent
    {
        public string SnapshotName;
        public float TransitionTime;
    }

    public struct ChangeSoundSignal : ISignalEvent
    {
        public int SoundId;
        public string NodePath;
    }

    public struct StopSoundSignal : ISignalEvent
    {
        public int SoundId;
    }

    public struct SoundFinishSignal : ISignalEvent
    {
        public int SoundId;
    }
}