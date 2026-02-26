using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Vars
{
    public sealed class VariableStore
    {
        private readonly Dictionary<string, object> _vars;

        public VariableStore()
        {
            _vars = new Dictionary<string, object>(16);
        }

        public Variable<T> Define<T>(string key, T value, bool replace = false)
        {
            if (_vars.TryGetValue(key, out var v))
            {
                var result = (Variable<T>)v;
                if (replace)
                {
                    result.Set(value);
                }
                else
                {
                    Debug.LogWarning($"Variable [{key}] is already defined.");
                }

                return result;
            }

            var variable = new Variable<T>(key, value);
            _vars[key] = variable;
            return variable;
        }

        public Variable<T> Define<T>(string key, Variable<T> value, bool replace = false)
        {
            if (_vars.TryGetValue(key, out var v))
            {
                if (!replace)
                {
                    Debug.LogWarning($"Variable [{key}] is already defined.");
                    return (Variable<T>)v;
                }
            }

            _vars[key] = value;
            return value;
        }

        public void Undefine(string key)
        {
            _vars.Remove(key);
        }

        public Variable<T> Get<T>(string key)
        {
            if (_vars.TryGetValue(key, out var v))
            {
                return (Variable<T>)v;
            }

            return null;
        }
        
        internal IEnumerable<object> All => _vars.Values;
    }
}