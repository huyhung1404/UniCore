#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;

namespace UniCore.Audio.Pool
{
    internal static class ClipDataPool
    {
        private static readonly Queue<ClipData> pool = new Queue<ClipData>(8);

        public static void Push(ClipData clip)
        {
            if (clip == null) return;
            clip.Clear();
            pool.Enqueue(clip);
        }

        public static ClipData Pop()
        {
            return pool.Count == 0 ? new ClipData() : pool.Dequeue();
        }
    }
}
#endif