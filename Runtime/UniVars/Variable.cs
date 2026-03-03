using System.Collections.Generic;
using UniCore.Signal;

namespace UniCore.Vars
{
    public interface IVariable
    {
        public int Hash { get; }
        public void ResetValue();
    }
    
    public class Variable<T> : IVariable
    {
        protected readonly int _hash;
        protected T _value;
        protected T _initialValue;
        protected bool _shouldDispatch;

#if UNITY_EDITOR
        protected readonly string _name;
#endif

        public int Hash => _hash;

        public Variable(int hash, T value, string name)
        {
            _hash = hash;
            _value = value;
            _initialValue = value;
            _shouldDispatch = false;
#if UNITY_EDITOR
            _name = name;
#endif
        }
        
        public Variable<T> WithChangeSignal(bool enable = true)
        {
            _shouldDispatch = enable;
            return this;
        }

        public virtual void Set(T v)
        {
            if (EqualityComparer<T>.Default.Equals(_value, v)) return;

            var old = _value;
            _value = v;
            
            if (!_shouldDispatch) return;

            SignalSystem.Dispatch(new VariableChangedSignal<T>
            {
                Hash = _hash,
                OldValue = old,
                NewValue = v
            });
        }

        public virtual T Get() => _value;

        public void ResetValue() => Set(_initialValue);

        public static implicit operator T(Variable<T> variable) => variable._value;
    }
}