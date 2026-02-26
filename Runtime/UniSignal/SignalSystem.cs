using System;
using System.Collections.Generic;

namespace UniCore.Signal
{
    public static class SignalSystem
    {
        internal static readonly Dictionary<Type, IListenerList> s_Listeners = new Dictionary<Type, IListenerList>(32);

        public static void Register<T>(ISignalListener<T> listener) where T : ISignalEvent
        {
            var type = typeof(T);
            Register(type, listener);
        }

        internal static void Register(Type signalType, object listener)
        {
            if (!s_Listeners.TryGetValue(signalType, out var raw))
            {
                raw = CreateList(signalType);
                s_Listeners[signalType] = raw;
            }

            raw.Add(listener);
        }

        private static IListenerList CreateList(Type t)
        {
            var listType = typeof(ListenerList<>).MakeGenericType(t);
            return (IListenerList)Activator.CreateInstance(listType);
        }

        public static void Unregister<T>(ISignalListener<T> listener) where T : ISignalEvent
        {
            var type = typeof(T);
            Unregister(type, listener);
        }

        internal static void Unregister(Type signalType, object listener)
        {
            if (s_Listeners.TryGetValue(signalType, out var raw)) raw.Remove(listener);
        }

        public static void Dispatch<T>(T signal) where T : ISignalEvent => Dispatch(signal, signal.Scope);

        public static void Dispatch<T>(T signal, SignalScope scope) where T : ISignalEvent
        {
            if (s_Listeners.TryGetValue(typeof(T), out var raw)) ((ListenerList<T>)raw).Dispatch(signal, scope);
        }

        public static void ReleaseEmptyLists()
        {
            if (s_Listeners.Count == 0) return;
            var temp = new List<Type>(16);
            foreach (var kvp in s_Listeners)
            {
                if (kvp.Value.Count == 0) temp.Add(kvp.Key);
            }

            for (var i = 0; i < temp.Count; i++) s_Listeners.Remove(temp[i]);
        }

        public static void Clear()
        {
            s_Listeners.Clear();
        }
    }
}