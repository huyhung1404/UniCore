using UniCore.Signal;

namespace UniCore.Vars
{
    public struct VariableChangedSignal<T> : ISignalEvent
    {
        public int Hash;
        public T OldValue;
        public T NewValue;
    }
}