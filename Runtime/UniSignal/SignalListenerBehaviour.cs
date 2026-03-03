using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Signal
{
    public class SignalListenerBehaviour : MonoBehaviour
    {
        private static readonly Type s_listenerGenericType = typeof(ISignalListener<>);
        private static Dictionary<Type, Type[]> s_typeCache;

        public virtual void OnEnable() => Auto(true);
        public virtual void OnDisable() => Auto(false);

        private void Auto(bool register)
        {
            var monoType = GetType();
            
            s_typeCache ??= new Dictionary<Type, Type[]>(8);
            if (!s_typeCache.TryGetValue(monoType, out var signals))
            {
                signals = BuildSignalArray(monoType);
                s_typeCache[monoType] = signals;
            }

            var length = signals.Length;
            for (var i = 0; i < length; i++)
            {
                if (register)
                    SignalSystem.Register(signals[i], this);
                else
                    SignalSystem.Unregister(signals[i], this);
            }
        }

        private static Type[] BuildSignalArray(Type t)
        {
            var interfaces = t.GetInterfaces();
            var list = new List<Type>(4);

            for (var i = interfaces.Length - 1; i >= 0; i--)
            {
                var itf = interfaces[i];
                if (!itf.IsGenericType) continue;
                if (itf.GetGenericTypeDefinition() != s_listenerGenericType) continue;

                list.Add(itf.GetGenericArguments()[0]);
            }

            return list.ToArray();
        }
    }
}