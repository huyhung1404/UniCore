#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Audio.Pool
{
    internal static class SoundEmitterPool
    {
        private static readonly Queue<SoundEmitter> s_pool = new Queue<SoundEmitter>(32);

        public static void Prewarm(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var o = Object.Instantiate(AudioSystem.s_Instance.Settings.SoundEmitterPrefab);
                Push(o);
            }
        }
        
        public static void Push(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            emitter.transform.parent = AudioSystem.s_Instance.transform;
            s_pool.Enqueue(emitter);
        }

        public static SoundEmitter Pop()
        {
            if (s_pool.Count <= 0) return Object.Instantiate(AudioSystem.s_Instance.Settings.SoundEmitterPrefab);
            var r = s_pool.Dequeue();
            r.gameObject.SetActive(true);
            return r;
        }
    }
}
#endif