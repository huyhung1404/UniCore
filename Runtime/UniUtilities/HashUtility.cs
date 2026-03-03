using System;

namespace UniCore.Utilities
{
    public static class HashUtility
    {
        private const int k_fnvOffsetBasis = unchecked((int)2166136261);
        private const int k_fnvPrime = 16777619;

        public static int GetFNV1aHash(this ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return 0;
            var hash = k_fnvOffsetBasis;
            foreach (var c in span)
            {
                hash ^= c;
                hash *= k_fnvPrime;
            }

            return hash;
        }

        public static int GetFNV1aHash(this string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.AsSpan().GetFNV1aHash();
        }
    }
}