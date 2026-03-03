namespace UniCore.Signal
{
    public interface IPoolableSignal : ISignalEvent
    {
        public void OnRelease();
    }
}