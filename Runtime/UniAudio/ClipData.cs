#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Audio
{
    public struct ClipPlayCommand
    {
        public AudioClip Clip;
        public float Delay;
        public AudioClipReference Reference;
        public float ReleaseDelay;
        public int LayerIndex;
        public float LayerVolume;
    }
    
    public class ClipData
    {
        public readonly List<ClipPlayCommand> Commands;

        public ClipData()
        {
            Commands = new List<ClipPlayCommand>(4);
        }

        public void Clear()
        {
            Commands.Clear();
        }
    }
}
#endif