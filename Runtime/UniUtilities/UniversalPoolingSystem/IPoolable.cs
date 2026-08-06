namespace UniCore.Utilities.Pooling
{
    public interface IPoolable
    {
        void OnRent();
        void OnReturn();
    }
}