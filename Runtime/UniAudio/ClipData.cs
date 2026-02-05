#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Audio
{
    public class ClipData
    {
        public readonly List<AudioClip> clips;

        public ClipData()
        {
            clips = new List<AudioClip>(4);
        }

        public void Clear()
        {
            clips.Clear();
        }
    }
}
#endif