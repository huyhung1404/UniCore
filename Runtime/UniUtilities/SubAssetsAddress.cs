using System;
using UnityEngine;

namespace UniCore.Utilities
{
    [Serializable]
    public class SubAssetsAddress
    {
        public SubAssetsAddress()
        {
        }

        public SubAssetsAddress(params string[] subs)
        {
            Subs = subs;
        }

        [SerializeField] private string[] Subs;

        public int Length() => Subs.Length;

        public string this[int index]
        {
            get
            {
                if (Subs == null || index < 0 || index >= Subs.Length)
                    return null;

                return Subs[index];
            }
        }
    }
}