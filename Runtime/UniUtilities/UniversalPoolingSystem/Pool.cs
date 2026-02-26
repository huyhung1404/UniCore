using System.Collections.Generic;

namespace UniCore.Utilities.Pooling
{
    public class Pool<T>
    {
        private readonly Stack<T> _stack;
        private readonly PoolPolicy<T> _policy;
        private readonly int _maxSize;

        public int CountInactive => _stack.Count;

        public Pool(PoolPolicy<T> policy, int initialSize = 0, int maxSize = 1000)
        {
            _policy = policy;
            _maxSize = maxSize;
            _stack = new Stack<T>(initialSize);

            for (var i = 0; i < initialSize; i++)
                _stack.Push(policy.Create());
        }

        public T Rent()
        {
            var item = _stack.Count > 0 ? _stack.Pop() : _policy.Create();
            _policy.OnGet(item);
            return item;
        }

        public void Return(T item)
        {
            _policy.OnRelease(item);

            if (_stack.Count < _maxSize)
                _stack.Push(item);
            else
                _policy.OnDestroy(item);
        }

        public void Clear()
        {
            while (_stack.Count > 0)
                _policy.OnDestroy(_stack.Pop());
        }
    }
}