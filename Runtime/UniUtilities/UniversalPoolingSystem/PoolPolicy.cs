namespace UniCore.Utilities.Pooling
{
    public abstract class PoolPolicy<T>
    {
        public abstract T Create();
        public virtual void OnGet(T obj) { }
        public virtual void OnRelease(T obj) { }
        public virtual void OnDestroy(T obj) { }
    }
}