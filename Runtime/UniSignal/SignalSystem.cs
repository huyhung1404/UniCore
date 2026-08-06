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

    internal static class SignalPoolCache<T> where T : IPoolableSignal
    {
        internal static Stack<T> s_pool;
    }

    public static class SignalSystem
    {
        internal static readonly List<IListenerList> s_ActiveLists = new List<IListenerList>(32);

#if SIGNAL_THREAD_SAFE
        private static readonly object s_lock = new object();
#endif

        public static void Register<T>(ISignalListener<T> listener) where T : ISignalEvent
        {
            var list = SignalCache<T>.s_list;
            if (list == null)
            {
                list = new ListenerList<T>();

#if SIGNAL_THREAD_SAFE
                lock (s_lock)
                {
                    if (!s_ActiveLists.Contains(list)) s_ActiveLists.Add(list);
                }
#else
                if (!s_ActiveLists.Contains(list)) s_ActiveLists.Add(list);
#endif
            }

            list.Add(listener);
        }

        public static void Unregister<T>(ISignalListener<T> listener) where T : ISignalEvent
        {
            SignalCache<T>.s_list?.Remove(listener);
        }

        [Obsolete("Performance Warning: Use the generic Register<T>(listener) instead for Zero-GC O(1) performance.", false)]
        internal static void Register(Type signalType, object listener)
        {
            var method = GetGenericMethod(nameof(Register), signalType);
            method?.Invoke(null, new[] { listener });
        }

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
                if (m.Name == methodName && m.IsGenericMethod) return m.MakeGenericMethod(genericType);
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
#if SIGNAL_THREAD_SAFE
            lock (s_lock)
            {
#endif
            for (var i = s_ActiveLists.Count - 1; i >= 0; i--)
            {
                var list = s_ActiveLists[i];
                if (list.Count != 0) continue;
                list.DestroyCache();
                s_ActiveLists.RemoveAt(i);
            }
#if SIGNAL_THREAD_SAFE
            }
#endif
        }

        public static void Clear()
        {
#if SIGNAL_THREAD_SAFE
            lock (s_lock)
            {
#endif
            var count = s_ActiveLists.Count;
            for (var i = 0; i < count; i++) s_ActiveLists[i].DestroyCache();
            s_ActiveLists.Clear();
#if SIGNAL_THREAD_SAFE
            }
#endif
        }

        public static T Get<T>() where T : IPoolableSignal, new()
        {
#if SIGNAL_THREAD_SAFE
            lock (s_lock)
            {
#endif
            var pool = SignalPoolCache<T>.s_pool;
            if (pool == null)
            {
                pool = new Stack<T>(8);
                SignalPoolCache<T>.s_pool = pool;
            }

            return pool.Count > 0 ? pool.Pop() : new T();
#if SIGNAL_THREAD_SAFE
            }
#endif
        }

        public static void Release<T>(T signal) where T : IPoolableSignal
        {
#if SIGNAL_THREAD_SAFE
            lock (s_lock)
            {
#endif
            var pool = SignalPoolCache<T>.s_pool;
            if (pool == null)
            {
                pool = new Stack<T>(8);
                SignalPoolCache<T>.s_pool = pool;
            }

            signal.OnRelease();
            pool.Push(signal);
#if SIGNAL_THREAD_SAFE
            }
#endif
        }
    }
}