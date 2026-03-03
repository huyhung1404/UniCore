using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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

        private int _spinLockIndicator;
        private int _dispatchCount;
        private volatile bool _needsCleanup;

        public ListenerList()
        {
            SignalCache<T>.s_list = this;
        }

        private void EnterWriteLock(ref SpinWait spinWait)
        {
            while (Interlocked.CompareExchange(ref _spinLockIndicator, 1, 0) != 0)
            {
                spinWait.SpinOnce();
            }
        }

        private void ExitWriteLock()
        {
            Volatile.Write(ref _spinLockIndicator, 0);
        }

        public void DestroyCache()
        {
            SignalCache<T>.s_list = null;

            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
                _list.Clear();
                _pendingAdds.Clear();
                _needsCleanup = false;
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public int Count
        {
            get
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try
                {
                    return _list.Count;
                }
                finally
                {
                    ExitWriteLock();
                }
            }
        }

        public object Get(int index)
        {
            var spinWait = new SpinWait();
            EnterWriteLock(ref spinWait);
            try
            {
                return _list[index];
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public void Add(object o)
        {
            var listener = (ISignalListener<T>)o;
            var spinWait = new SpinWait();

            EnterWriteLock(ref spinWait);
            try
            {
                if (_list.Contains(listener) || _pendingAdds.Contains(listener)) return;

                if (Volatile.Read(ref _dispatchCount) > 0)
                {
                    _pendingAdds.Add(listener);
                    _needsCleanup = true;
                }
                else
                {
                    InsertSorted(listener);
                }
            }
            finally
            {
                ExitWriteLock();
            }
        }

        public void Remove(object o)
        {
            var listener = (ISignalListener<T>)o;
            var spinWait = new SpinWait();

            EnterWriteLock(ref spinWait);
            try
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
            finally
            {
                ExitWriteLock();
            }
        }

        public void Dispatch(T signal, SignalScope scope)
        {
            Interlocked.Increment(ref _dispatchCount);
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

            if (Interlocked.Decrement(ref _dispatchCount) == 0 && _needsCleanup)
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try
                {
                    if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications();
                }
                finally
                {
                    ExitWriteLock();
                }
            }
        }

        public async ValueTask DispatchAsync(T signal, SignalScope scope)
        {
            Interlocked.Increment(ref _dispatchCount);

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
                    if (listener is IAsyncSignalListener<T> asyncListener)
                    {
                        await asyncListener.OnSignalAsync(signal);
                    }
                    else
                    {
                        listener.OnSignal(signal);
                    }

                    if (listener.IsOneShot) Remove(listener);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UniSignal] Async Exception: {ex}");
                }
            }

            if (Interlocked.Decrement(ref _dispatchCount) == 0 && _needsCleanup)
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try
                {
                    if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications();
                }
                finally
                {
                    ExitWriteLock();
                }
            }
        }

        public async ValueTask DispatchParallelAsync(T signal, SignalScope scope)
        {
            Interlocked.Increment(ref _dispatchCount);

            var count = _list.Count;
            if (count == 0)
            {
                Interlocked.Decrement(ref _dispatchCount);
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
                    if (listener is IAsyncSignalListener<T> asyncListener)
                    {
                        taskList[taskCount++] = asyncListener.OnSignalAsync(signal).AsTask();
                    }
                    else
                    {
                        listener.OnSignal(signal);
                    }

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

            if (Interlocked.Decrement(ref _dispatchCount) == 0 && _needsCleanup)
            {
                var spinWait = new SpinWait();
                EnterWriteLock(ref spinWait);
                try
                {
                    if (_dispatchCount == 0 && _needsCleanup) ApplyPendingModifications();
                }
                finally
                {
                    ExitWriteLock();
                }
            }
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

            if (aliveCount < currentCount)
            {
                _list.RemoveRange(aliveCount, currentCount - aliveCount);
            }

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