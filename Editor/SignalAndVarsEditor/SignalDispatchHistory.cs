using System;
using System.Collections.Generic;
using UniCore.Signal;

namespace UniCore.Editor
{
    internal static class SignalDispatchHistory
    {
        private const int k_maxRecords = 50;
        private static readonly Queue<string> s_records = new(k_maxRecords);

        public static void Record(Type signalType, SignalScope scope)
        {
            if (s_records.Count >= k_maxRecords)
                s_records.Dequeue();

            s_records.Enqueue(
                $"[{UnityEngine.Time.frameCount}] {signalType.Name} | Scope: {scope}"
            );
        }

        public static IEnumerable<string> Records => s_records;
        public static void Clear() => s_records.Clear();
    }
}