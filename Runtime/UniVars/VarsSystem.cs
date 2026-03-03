using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Vars
{
    public static class VarsSystem
    {
        public static StoreAccessor Store { get; }

        static VarsSystem()
        {
            Store = new StoreAccessor();
        }
        
        public static VariableStore DefineScope(string scopeName) => Store.DefineScope(scopeName);
        public static bool UndefineScope(string scopeName) => Store.UndefineScope(scopeName);
        internal static IEnumerable<(string Name, VariableStore Store)> AllStores => Store.GetAllStores();
    }
    
    public sealed class StoreAccessor : VariableStore
    {
        private readonly Dictionary<string, VariableStore> _scopes;
        private const string k_GlobalKey = "Global";
        
        public StoreAccessor()
        {
            _scopes = new Dictionary<string, VariableStore>(8, System.StringComparer.Ordinal);
            _scopes.Add(k_GlobalKey, this);
        }
        
        public VariableStore this[string scopeName]
        {
            get
            {
                if (string.IsNullOrEmpty(scopeName) || scopeName == k_GlobalKey) return this;
                if (_scopes.TryGetValue(scopeName, out var store)) return store;
                Debug.LogWarning($"[UniVars] Scope '{scopeName}' not found. Returning Global instead.");
                return this;
            }
        }

        public VariableStore DefineScope(string scopeName)
        {
            if (string.IsNullOrEmpty(scopeName)) return null;

            if (_scopes.TryGetValue(scopeName, out var existing)) return existing;

            var newStore = new VariableStore();
            _scopes.Add(scopeName, newStore);
            return newStore;
        }

        public bool UndefineScope(string scopeName)
        {
            if (scopeName == k_GlobalKey)
            {
                Debug.LogWarning("[VarsSystem] 🛡️ Protection: Cannot remove Global scope!");
                return false;
            }

            return _scopes.Remove(scopeName);
        }

        internal IEnumerable<(string, VariableStore)> GetAllStores()
        {
            foreach (var kvp in _scopes)
            {
                yield return (kvp.Key, kvp.Value);
            }
        }
    }
}