namespace UniCore.Utilities.Pooling
{
    public interface IPoolable
    {
        public void OnRent();
        public void OnReturn();
    }
}