#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Audio
{
    public class ClipData
    {
        public readonly List<AudioClip> Clips;

        public ClipData()
        {
            Clips = new List<AudioClip>(4);
        }

        public void Clear()
        {
            Clips.Clear();
        }
    }
}
#endif