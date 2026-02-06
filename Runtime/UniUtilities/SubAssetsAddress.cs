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
            this.subs = subs;
        }

        [SerializeField] private string[] subs;

        public int Length() => subs.Length;

        public string this[int index]
        {
            get
            {
                if (subs == null || index < 0 || index >= subs.Length)
                    return null;

                return subs[index];
            }
        }
    }
}