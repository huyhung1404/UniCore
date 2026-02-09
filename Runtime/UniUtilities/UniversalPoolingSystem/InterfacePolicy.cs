using System;

namespace UniCore.Utilities.Pooling
{
    public class InterfacePolicy<T> : PoolPolicy<T>
    {
        private readonly Func<T> factory;

        public InterfacePolicy(Func<T> factory)
        {
            this.factory = factory;
        }

        public override T Create() => factory();

        public override void OnGet(T obj)
        {
            if (obj is IPoolable p) p.OnRent();
        }

        public override void OnRelease(T obj)
        {
            if (obj is IPoolable p) p.OnReturn();
        }
    }
}