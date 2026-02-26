using System.Collections.Generic;
using UniCore.Signal;

namespace UniCore.Vars
{
    public class Variable<T>
    {
        protected readonly string _key;
        protected T _value;

        public Variable(string key, T value)
        {
            _key = key;
            _value = value;
        }

        public virtual void Set(T v)
        {
            if (EqualityComparer<T>.Default.Equals(_value, v)) return;

            var old = _value;
            _value = v;

            SignalSystem.Dispatch(new VariableChangedSignal<T>
            {
                Key = _key,
                OldValue = old,
                NewValue = v
            });
        }

        public virtual T Get()
        {
            return _value;
        }

        public static implicit operator T(Variable<T> variable)
        {
            return variable._value;
        }
    }
}