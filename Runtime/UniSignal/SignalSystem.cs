using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace UniCore.Signal
{
    internal static class SignalCache<T> where T : ISignalEvent
    {
        internal static ListenerList<T> s_list;
    }

    public static class SignalSystem
    {
        internal static readonly List<IListenerList> s_ActiveLists = new(32);
        private static readonly object s_clearLock = new object();
        
        private static Dictionary<Type, object> s_pools;

        public static void Register<T>(ISignalListener<T> listener) where T : ISignalEvent
        {
            var list = SignalCache<T>.s_list;
            if (list == null)
            {
                list = new ListenerList<T>();

                lock (s_clearLock)
                {
                    if (!s_ActiveLists.Contains(list))
                    {
                        s_ActiveLists.Add(list);
                    }
                }
            }

            list.Add(listener);
        }

        public static void Unregister<T>(ISignalListener<T> listener) where T : ISignalEvent
        {
            SignalCache<T>.s_list?.Remove(listener);
        }

        /// <summary>
        /// Registers a listener using Reflection. 
        /// </summary>
        /// <remarks>
        /// <para><c>PERFORMANCE WARNING:</c></para>
        /// This method uses <c>MethodInfo.Invoke</c> which allocates an <c>object[]</c> array on the Heap, causing GC spikes.
        /// It should ONLY be used for Editor tooling or one-time initialization. For gameplay logic, use <see cref="Register{T}(ISignalListener{T})"/> instead.
        /// </remarks>
        [Obsolete("Performance Warning: Use the generic Register<T>(listener) instead for Zero-GC O(1) performance.", false)]
        internal static void Register(Type signalType, object listener)
        {
            var method = GetGenericMethod(nameof(Register), signalType);
            method?.Invoke(null, new[] { listener });
        }

        /// <summary>
        /// Unregister a listener using Reflection. 
        /// </summary>
        /// <remarks>
        /// <para><c>PERFORMANCE WARNING:</c></para>
        /// This method uses <c>MethodInfo.Invoke</c> which allocates an <c>object[]</c> array on the Heap, causing GC spikes.
        /// It should ONLY be used for Editor tooling or one-time initialization. For gameplay logic, use <see cref="Register{T}(ISignalListener{T})"/> instead.
        /// </remarks>
        [Obsolete("Performance Warning: Use the generic Register<T>(listener) instead for Zero-GC O(1) performance.", false)]
        internal static void Unregister(Type signalType, object listener)
        {
            var method = GetGenericMethod(nameof(Unregister), signalType);
            method?.Invoke(null, new[] { listener });
        }

        private static MethodInfo GetGenericMethod(string methodName, Type genericType)
        {
            var methods = typeof(SignalSystem).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var length = methods.Length;

            for (var i = 0; i < length; i++)
            {
                var m = methods[i];
                if (m.Name == methodName && m.IsGenericMethod)
                {
                    return m.MakeGenericMethod(genericType);
                }
            }

            return null;
        }


        public static void Dispatch<T>(T signal) where T : ISignalEvent => Dispatch(signal, signal.Scope);

        public static void Dispatch<T>(T signal, SignalScope scope) where T : ISignalEvent
        {
            SignalCache<T>.s_list?.Dispatch(signal, scope);
        }

        public static async ValueTask DispatchAsync<T>(T signal) where T : ISignalEvent => await DispatchAsync(signal, signal.Scope);

        public static async ValueTask DispatchAsync<T>(T signal, SignalScope scope) where T : ISignalEvent
        {
            var list = SignalCache<T>.s_list;
            if (list != null) await list.DispatchAsync(signal, scope);
        }

        public static async ValueTask DispatchParallelAsync<T>(T signal) where T : ISignalEvent => await DispatchParallelAsync(signal, signal.Scope);

        public static async ValueTask DispatchParallelAsync<T>(T signal, SignalScope scope) where T : ISignalEvent
        {
            var list = SignalCache<T>.s_list;
            if (list != null) await list.DispatchParallelAsync(signal, scope);
        }

        public static void ReleaseEmptyLists()
        {
            lock (s_clearLock)
            {
                for (var i = s_ActiveLists.Count - 1; i >= 0; i--)
                {
                    var list = s_ActiveLists[i];
                    if (list.Count != 0) continue;
                    list.DestroyCache();
                    s_ActiveLists.RemoveAt(i);
                }
            }
        }

        public static void Clear()
        {
            lock (s_clearLock)
            {
                var count = s_ActiveLists.Count;
                for (var i = 0; i < count; i++)
                {
                    s_ActiveLists[i].DestroyCache();
                }

                s_ActiveLists.Clear();
            }
        }

        public static T Get<T>() where T : IPoolableSignal, new()
        {
            var type = typeof(T);
            s_pools ??= new Dictionary<Type, object>(16);
            if (!s_pools.TryGetValue(type, out var poolObj))
            {
                poolObj = new Stack<T>(8);
                s_pools[type] = poolObj;
            }

            var pool = (Stack<T>)poolObj;
            return pool.Count > 0 ? pool.Pop() : new T();
        }

        public static void Release<T>(T signal) where T : IPoolableSignal
        {
            var type = typeof(T);
            s_pools ??= new Dictionary<Type, object>(16);
            if (s_pools.TryGetValue(type, out var poolObj))
            {
                signal.OnRelease();
                ((Stack<T>)poolObj).Push(signal);
            }
        }
    }
}