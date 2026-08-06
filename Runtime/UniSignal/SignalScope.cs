using System;
using System.Collections.Generic;

namespace UniCore.Signal
{
    /// <summary>
    /// Represents a scope of signals identified by a unique mask for filtering
    /// and handling specific signals in the UniSignal system.
    /// </summary>
    /// <remarks>
    /// This struct is used to define the scope in which a signal listener operates.
    /// The mask system allows bitwise operations to evaluate and combine signal scopes.
    /// </remarks>
    /// <seealso cref="ISignalListener{T}"/>
    /// <seealso cref="ISignalEvent"/>
    public readonly struct SignalScope : IEquatable<SignalScope>
    {
        public readonly ulong Mask;

        public SignalScope(ulong mask)
        {
            Mask = mask;
        }

        public static readonly SignalScope All = new SignalScope(ulong.MaxValue);

        public static SignalScope operator |(SignalScope a, SignalScope b) => new SignalScope(a.Mask | b.Mask);

        public bool Intersects(SignalScope other) => (Mask & other.Mask) != 0;

        public bool Equals(SignalScope other)
        {
            return Mask == other.Mask;
        }

        public override bool Equals(object obj)
        {
            return obj is SignalScope other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Mask.GetHashCode();
        }

        public override string ToString() => $"0x{Mask:X}";
    }

    public static class SignalScopeRegistry
    {
#if UNITY_EDITOR
        internal static readonly Dictionary<ulong, string> scopeNames = new Dictionary<ulong, string>(8)
        {
            { ulong.MaxValue, "All" }
        };
#endif

        public static void Register(string name, SignalScope scope)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(name))
                return;

            scopeNames[scope.Mask] = name;
#endif
        }

#if UNITY_EDITOR
        public static string GetName(SignalScope scope)
        {
            return scopeNames.TryGetValue(scope.Mask, out var name)
                ? name
                : $"0x{scope.Mask:X}";
        }

        public static IEnumerable<string> GetNames(SignalScope scope)
        {
            foreach (var kvp in scopeNames)
            {
                if (kvp.Key == ulong.MaxValue) continue;
                if ((kvp.Key & scope.Mask) != 0) yield return kvp.Value;
            }
        }

        public static string GetReadableScope(SignalScope scope)
        {
            if (scope.Mask == 0) return "None";
            if (scope.Mask == ulong.MaxValue) return "All";

            var names = GetNames(scope);
            var result = string.Join(" | ", names);

            return string.IsNullOrEmpty(result)
                ? $"0x{scope.Mask:X}"
                : result;
        }
#endif
    }
}