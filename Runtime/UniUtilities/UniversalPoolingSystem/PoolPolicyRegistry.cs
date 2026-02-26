using System;
using System.Collections.Generic;

namespace UniCore.Utilities.Pooling
{
    public class PoolPolicyRegistry
    {
        private static readonly Dictionary<object, object> s_policies = new();

        public static void Register<T>(object key, PoolPolicy<T> policy)
        {
            s_policies[key] = policy;
        }

        public static PoolPolicy<T> Get<T>(object key)
        {
            if (s_policies.TryGetValue(key, out var policy))
                return (PoolPolicy<T>)policy;

            throw new Exception($"No policy registered for key {key}");
        }
    }
}