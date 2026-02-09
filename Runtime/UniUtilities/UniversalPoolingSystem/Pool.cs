using System.Collections.Generic;

namespace UniCore.Utilities.Pooling
{
    public class Pool<T>
    {
        private readonly Stack<T> stack;
        private readonly PoolPolicy<T> policy;
        private readonly int maxSize;

        public int CountInactive => stack.Count;

        public Pool(PoolPolicy<T> policy, int initialSize = 0, int maxSize = 1000)
        {
            this.policy = policy;
            this.maxSize = maxSize;
            stack = new Stack<T>(initialSize);

            for (var i = 0; i < initialSize; i++)
                stack.Push(policy.Create());
        }

        public T Rent()
        {
            var item = stack.Count > 0 ? stack.Pop() : policy.Create();
            policy.OnGet(item);
            return item;
        }

        public void Return(T item)
        {
            policy.OnRelease(item);

            if (stack.Count < maxSize)
                stack.Push(item);
            else
                policy.OnDestroy(item);
        }

        public void Clear()
        {
            while (stack.Count > 0)
                policy.OnDestroy(stack.Pop());
        }
    }
}