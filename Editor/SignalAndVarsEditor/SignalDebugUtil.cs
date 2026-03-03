using System;
using System.Runtime.CompilerServices;
using UniCore.Signal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniCore.Editor.Windows
{
    public static class SignalDebugUtil
    {
        public static string GetSource(object listener)
        {
            if (listener is not MonoBehaviour mb) return "Non-Mono";
            var scene = mb.gameObject.scene;
            return scene.IsValid() ? $"{scene.name} / {mb.gameObject.name}" : "No Scene";
        }

        public static bool TryGetUnityObject(object listener, out Object obj)
        {
            if (listener is MonoBehaviour mb)
            {
                obj = mb.gameObject;
                return true;
            }

            obj = null;
            return false;
        }
        private class ListenerMeta
        {
            public int Priority;
            public string ScopeText;
            public SignalScope Scope;
        }

        private static readonly ConditionalWeakTable<object, ListenerMeta> s_metaCache = new();

        public static int GetPriority(object listener, Type signalType)
        {
            return GetOrCreateMeta(listener, signalType).Priority;
        }

        public static string GetScopeText(object listener, Type signalType)
        {
            return GetOrCreateMeta(listener, signalType).ScopeText;
        }

        public static SignalScope GetScope(object listener, Type signalType)
        {
            return GetOrCreateMeta(listener, signalType).Scope;
        }

        private static ListenerMeta GetOrCreateMeta(object listener, Type signalType)
        {
            if (s_metaCache.TryGetValue(listener, out var meta)) return meta;

            var newMeta = new ListenerMeta
            {
                Priority = 0,
                Scope = SignalScope.All,
                ScopeText = "All"
            };

            var listenerType = listener.GetType();
            var interfaces = listenerType.GetInterfaces();

            foreach (var itf in interfaces)
            {
                if (!itf.IsGenericType || itf.GetGenericTypeDefinition() != typeof(ISignalListener<>)) continue;
                
                var genericArgs = itf.GetGenericArguments();
                if (genericArgs.Length == 0 || genericArgs[0] != signalType) continue;

                var priorityProp = itf.GetProperty("Priority");
                if (priorityProp != null)
                {
                    newMeta.Priority = (int)priorityProp.GetValue(listener);
                }

                var scopeProp = itf.GetProperty("ListenScope");
                if (scopeProp != null)
                {
                    var scopeValue = (SignalScope)scopeProp.GetValue(listener);
                    newMeta.Scope = scopeValue;
#if UNITY_EDITOR
                    newMeta.ScopeText = SignalScopeRegistry.GetReadableScope(scopeValue);
#else
                    newMeta.ScopeText = scopeValue.ToString();
#endif
                }
                break; 
            }

            s_metaCache.Add(listener, newMeta);
            return newMeta;
        }
    }
}