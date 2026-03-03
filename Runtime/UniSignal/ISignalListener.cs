namespace UniCore.Signal
{
    /// <summary>
    /// Represents a listener that can handle signals of a specific type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of signal event that the listener can process. Must implement <see cref="ISignalEvent"/>.
    /// </typeparam>
    public interface ISignalListener<in T> where T : ISignalEvent
    {
        /// <summary>
        /// Lower values are executed later.
        /// </summary>
        public int Priority => 0;

        public SignalScope ListenScope => SignalScope.All;
        public bool IsOneShot => false;
        public void OnSignal(T signal);
    }
}