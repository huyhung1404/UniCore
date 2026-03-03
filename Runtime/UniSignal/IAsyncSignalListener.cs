using System.Threading.Tasks;

namespace UniCore.Signal
{
    public interface IAsyncSignalListener<in T> : ISignalListener<T> where T : ISignalEvent
    {
        void ISignalListener<T>.OnSignal(T signal) => OnSignalAsync(signal).Forget();
        public ValueTask OnSignalAsync(T signal);
    }

    public static class SignalTaskExtensions
    {
        public static async void Forget(this ValueTask task)
        {
            try { await task; }
            catch (System.Exception ex) { UnityEngine.Debug.LogException(ex); }
        }
    }
}