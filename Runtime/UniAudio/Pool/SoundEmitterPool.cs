#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Audio.Pool
{
    internal static class SoundEmitterPool
    {
        private static readonly Queue<SoundEmitter> pool = new Queue<SoundEmitter>(32);

        public static void Prewarm(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var o = Object.Instantiate(AudioSystem.instance.settings.SoundEmitterPrefab);
                Push(o);
            }
        }
        
        public static void Push(SoundEmitter emitter)
        {
            emitter.gameObject.SetActive(false);
            emitter.transform.parent = AudioSystem.instance.transform;
            pool.Enqueue(emitter);
        }

        public static SoundEmitter Pop()
        {
            if (pool.Count <= 0) return Object.Instantiate(AudioSystem.instance.settings.SoundEmitterPrefab);
            var r = pool.Dequeue();
            r.gameObject.SetActive(true);
            return r;
        }
    }
}
#endif