namespace UniCore.Utilities.Pooling
{
    public class ClassPolicy<T> : PoolPolicy<T> where T : class, new()
    {
        public override T Create() => new();

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