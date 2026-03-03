namespace UniCore.Signal
{
    public interface IConsumableSignal : ISignalEvent
    {
        public bool IsConsumed { get; set; }
    }
}