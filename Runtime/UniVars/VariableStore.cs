using System.Collections.Generic;
using UniCore.Utilities;
using UnityEngine;

namespace UniCore.Vars
{
    public class VariableStore
    {
        private readonly Dictionary<int, IVariable> _vars = new Dictionary<int, IVariable>(32);

        public Variable<T> Define<T>(string key, T value, bool replace = false)
        {
            var hash = key.GetFNV1aHash();
            
            if (!_vars.TryGetValue(hash, out var v))
            {
                var variable = new Variable<T>(hash, value, key);
                _vars[hash] = variable;
                return variable;
            }

            if (v is not Variable<T> typedVar)
            {
                Debug.LogError($"[VariableStore] Type mismatch for key '{key}'.");
                return null;
            }

            if (replace) typedVar.Set(value);
            return typedVar;
        }

        public Variable<T> Get<T>(int hash)
        {
            if (_vars.TryGetValue(hash, out var v)) return v as Variable<T>;
            return null;
        }

        public Variable<T> Get<T>(string key) => Get<T>(key.GetFNV1aHash());
        
        public void ResetAll()
        {
            foreach (var v in _vars.Values) v.ResetValue();
        }

        internal IEnumerable<IVariable> All => _vars.Values;
    }
}