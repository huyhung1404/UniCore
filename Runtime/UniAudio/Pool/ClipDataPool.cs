#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;

namespace UniCore.Audio.Pool
{
    internal static class ClipDataPool
    {
        private static readonly Queue<ClipData> s_pool = new Queue<ClipData>(8);

        public static void Push(ClipData clip)
        {
            if (clip == null) return;
            clip.Clear();
            s_pool.Enqueue(clip);
        }

        public static ClipData Pop()
        {
            return s_pool.Count == 0 ? new ClipData() : s_pool.Dequeue();
        }
    }
}
#endif