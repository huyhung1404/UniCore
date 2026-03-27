using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if SIGNAL_THREAD_SAFE
using System.Threading;
#endif

namespace UniCore.Signal
{
    internal interface IListenerList
    {
#if UNITY_EDITOR
        Type SignalType { get; }
#endif
        int Count { get; }
        object Get(int index);
        void DestroyCache();
    }

    internal sealed class ListenerList<T> : IListenerList where T : ISignalEvent
    {
#if UNITY_EDITOR
        public Type SignalType => typeof(T);
#endif
        private readonly List<ISignalListener<T>> _list = new(8);
        private readonly List<ISignalListener<T>> _pendingAdds = new(4);

#if SIGNAL_THREAD_SAFE
        private int _spinLockIndicator;
        private volatile bool _needsCleanup;
#else
        private bool _needsCleanup;
#endif
        private int _dispatchCount;

        public ListenerList()
        {
            SignalCache<T>.s_list = this;
        }

#if SIGNAL_THREAD_SAFE
        private void EnterWriteLock(ref SpinWait spinWait)
        {
            while (Interlocked.CompareExchange(ref _spinLockIndicator, 1, 0) != 0) spinWait.SpinOnce();
        }

        private void ExitWriteLock()
        {
            Volatile.Write(ref _spinLockIndicator, 0);
        }
#endif

        public void DestroyCache()
        {
            SignalCache<T>.s_list = null;

#if SIGNAL_THREAD_SAFE
            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
#endif
            _list.Clear();
            _pendingAdds.Clear();
            _needsCleanup = false;
#if SIGNAL_THREAD_SAFE
            }
            finally { ExitWriteLock(); }
#endif
        }

        public int Count
        {
            get
            {
#if SIGNAL_THREAD_SAFE
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try { return _list.Count; }
                finally { ExitWriteLock(); }
#else
                return _list.Count;
#endif
            }
        }

        public object Get(int index)
        {
#if SIGNAL_THREAD_SAFE
            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try { return _list[index]; }
            finally { ExitWriteLock(); }
#else
            return _list[index];
#endif
        }

        public void Add(object o)
        {
            var listener = (ISignalListener<T>)o;

#if SIGNAL_THREAD_SAFE
            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
#endif
            if (_list.Contains(listener) || _pendingAdds.Contains(listener)) return;

#if SIGNAL_THREAD_SAFE
                if (Volatile.Read(ref _dispatchCount) > 0)
#else
            if (_dispatchCount > 0)
#endif
            {
                _pendingAdds.Add(listener);
                _needsCleanup = true;
            }
            else
            {
                InsertSorted(listener);
            }
#if SIGNAL_THREAD_SAFE
            }
            finally { ExitWriteLock(); }
#endif
        }

        public void Remove(object o)
        {
            var listener = (ISignalListener<T>)o;

#if SIGNAL_THREAD_SAFE
            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
                if (Volatile.Read(ref _dispatchCount) > 0)
#else
            if (_dispatchCount > 0)
#endif
            {
                var idx = _list.IndexOf(listener);
                if (idx >= 0)
                {
                    _list[idx] = null;
                    _needsCleanup = true;
                }
                else
                {
                    _pendingAdds.Remove(listener);
                }
            }
            else
            {
                _list.Remove(listener);
            }
#if SIGNAL_THREAD_SAFE
            }
            finally { ExitWriteLock(); }
#endif
        }

        public void Dispatch(T signal, SignalScope scope)
        {
#if SIGNAL_THREAD_SAFE
            Interlocked.Increment(ref _dispatchCount);
#else
            _dispatchCount++;
#endif
            var consumable = signal as IConsumableSignal;
            var count = _list.Count;

            for (var i = 0; i < count; i++)
            {
                var listener = _list[i];
                if (listener == null) continue;
                if (!listener.ListenScope.Intersects(scope)) continue;
                if (consumable != null && consumable.IsConsumed) break;

                try
                {
                    listener.OnSignal(signal);
                    if (listener.IsOneShot) Remove(listener);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

#if SIGNAL_THREAD_SAFE
            if (Interlocked.Decrement(ref _dispatchCount) == 0 && _needsCleanup)
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try { if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications(); }
                finally { ExitWriteLock(); }
            }
#else
            _dispatchCount--;
            if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications();
#endif
        }

        public async ValueTask DispatchAsync(T signal, SignalScope scope)
        {
#if SIGNAL_THREAD_SAFE
            Interlocked.Increment(ref _dispatchCount);
#else
            _dispatchCount++;
#endif
            var consumable = signal as IConsumableSignal;
            var count = _list.Count;

            for (var i = 0; i < count; i++)
            {
                var listener = _list[i];
                if (listener == null) continue;
                if (!listener.ListenScope.Intersects(scope)) continue;
                if (consumable != null && consumable.IsConsumed) break;

                try
                {
                    if (listener is IAsyncSignalListener<T> asyncListener) await asyncListener.OnSignalAsync(signal);
                    else listener.OnSignal(signal);

                    if (listener.IsOneShot) Remove(listener);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UniSignal] Async Exception: {ex}");
                }
            }

#if SIGNAL_THREAD_SAFE
            if (Interlocked.Decrement(ref _dispatchCount) == 0 && _needsCleanup)
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try { if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications(); }
                finally { ExitWriteLock(); }
            }
#else
            _dispatchCount--;
            if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications();
#endif
        }

        public async ValueTask DispatchParallelAsync(T signal, SignalScope scope)
        {
#if SIGNAL_THREAD_SAFE
            Interlocked.Increment(ref _dispatchCount);
#else
            _dispatchCount++;
#endif
            var count = _list.Count;

            if (count == 0)
            {
#if SIGNAL_THREAD_SAFE
                Interlocked.Decrement(ref _dispatchCount);
#else
                _dispatchCount--;
#endif
                return;
            }

            var taskList = new Task[count];
            var taskCount = 0;

            for (var i = 0; i < count; i++)
            {
                var listener = _list[i];
                if (listener == null) continue;
                if (!listener.ListenScope.Intersects(scope)) continue;

                try
                {
                    if (listener is IAsyncSignalListener<T> asyncListener) taskList[taskCount++] = asyncListener.OnSignalAsync(signal).AsTask();
                    else listener.OnSignal(signal);

                    if (listener.IsOneShot) Remove(listener);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UniSignal] Parallel Async Exception: {ex}");
                }
            }

            if (taskCount > 0)
            {
                var tasksToWait = new Task[taskCount];
                Array.Copy(taskList, tasksToWait, taskCount);
                await Task.WhenAll(tasksToWait);
            }

#if SIGNAL_THREAD_SAFE
            if (Interlocked.Decrement(ref _dispatchCount) == 0 && _needsCleanup)
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try { if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications(); }
                finally { ExitWriteLock(); }
            }
#else
            _dispatchCount--;
            if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications();
#endif
        }

        private void ApplyPendingModifications()
        {
            var aliveCount = 0;
            var currentCount = _list.Count;
            for (var i = 0; i < currentCount; i++)
            {
                var item = _list[i];
                if (item != null)
                {
                    if (i != aliveCount) _list[aliveCount] = item;
                    aliveCount++;
                }
            }

            if (aliveCount < currentCount) _list.RemoveRange(aliveCount, currentCount - aliveCount);

            var pendingCount = _pendingAdds.Count;
            if (pendingCount > 0)
            {
                for (var i = 0; i < pendingCount; i++) InsertSorted(_pendingAdds[i]);
                _pendingAdds.Clear();
            }

            _needsCleanup = false;
        }

        private void InsertSorted(ISignalListener<T> listener)
        {
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
    }
}