using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UniCore.Signal
{
    internal interface IListenerList
    {
        public int Count { get; }
        public object Get(int index);
        public void Add(object listener);
        public void Remove(object listener);
    }

    internal sealed class ListenerList<T> : IListenerList where T : ISignalEvent
    {
        private readonly List<ISignalListener<T>> _list = new(8);
        
        private List<ISignalListener<T>> _pendingAdds;
        private List<ISignalListener<T>> _pendingRemoves;
        
        private bool _isDispatching;

        public int Count => _list.Count;

        public object Get(int index) => _list[index];

        public void Add(object o)
        {
            var listener = (ISignalListener<T>)o;

            if (_isDispatching)
            {
                _pendingAdds ??= new List<ISignalListener<T>>(4);
                if (!_pendingAdds.Contains(listener)) _pendingAdds.Add(listener);
                return;
            }

            if (_list.Contains(listener)) return;

            var p = listener.Priority;
            var i = _list.Count;
            _list.Add(listener);
            
            while (i > 0 && _list[i - 1].Priority < p)
            {
                _list[i] = _list[i - 1];
                i--;
            }

            _list[i] = listener;
        }

        public void Remove(object o)
        {
            var listener = (ISignalListener<T>)o;
            
            if (_isDispatching)
            {
                _pendingRemoves ??= new List<ISignalListener<T>>(4);
                if (!_pendingRemoves.Contains(listener)) _pendingRemoves.Add(listener);
                return;
            }
            
            _list.Remove(listener);
        }

        public void Dispatch(T signal, SignalScope scope)
        {
            _isDispatching = true;
            var c = _list.Count;

            for (var i = 0; i < c; i++)
            {
                var listener = _list[i];
                
                if (_pendingRemoves != null && _pendingRemoves.Contains(listener)) continue;
                if (!listener.ListenScope.Intersects(scope)) continue;

                try
                {
                    listener.OnSignal(signal);
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[UniSignal] Exception in {listener.GetType().Name} " +
                        $"while handling {typeof(T).Name}\n{ex}"
                    );
                }
            }

            _isDispatching = false;
            ApplyPendingModifications();
        }

        public async ValueTask DispatchAsync(T signal, SignalScope scope)
        {
            _isDispatching = true;
            var count = _list.Count;

            for (var i = 0; i < count; i++)
            {
                var listener = _list[i];
                
                if (_pendingRemoves != null && _pendingRemoves.Contains(listener)) continue;
                if (!listener.ListenScope.Intersects(scope)) continue;

                try
                {
                    if (listener is IAsyncSignalListener<T> asyncListener)
                    {
                        await asyncListener.OnSignalAsync(signal);
                    }
                    else
                    {
                        listener.OnSignal(signal);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[UniSignal] Async Exception: {ex}");
                }
            }

            _isDispatching = false;
            ApplyPendingModifications();
        }

        private void ApplyPendingModifications()
        {
            if (_pendingRemoves != null && _pendingRemoves.Count > 0)
            {
                var removeCount = _pendingRemoves.Count;
                for (var i = 0; i < removeCount; i++) _list.Remove(_pendingRemoves[i]);
                _pendingRemoves.Clear();
            }

            if (_pendingAdds != null && _pendingAdds.Count > 0)
            {
                var addCount = _pendingAdds.Count;
                for (var i = 0; i < addCount; i++) Add(_pendingAdds[i]);
                _pendingAdds.Clear();
            }
        }
    }
}