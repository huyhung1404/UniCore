using System.Collections.Generic;

namespace UniCore.Utilities.Pooling
{
    public static class PoolManager
    {
        private static readonly Dictionary<object, object> pools = new();

        public static Pool<T> GetPool<T>(object key, int init = 0)
        {
            if (pools.TryGetValue(key, out var existing))
                return (Pool<T>)existing;

            var policy = PoolPolicyRegistry.Get<T>(key);
            var pool = new Pool<T>(policy, init);

            pools[key] = pool;
            return pool;
        }
    }
}