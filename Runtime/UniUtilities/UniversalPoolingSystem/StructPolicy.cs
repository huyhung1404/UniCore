namespace UniCore.Utilities.Pooling
{
    public class StructPolicy<T> : PoolPolicy<T> where T : struct
    {
        public override T Create() => default;
    }
}