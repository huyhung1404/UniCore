#if HAS_UNITASK && HAS_ADDRESSABLES
using System;

namespace UniCore.Audio
{
    [Serializable]
    public enum AddressGroupType
    {
        Static,
        Custom
    }

    public interface IAddressGroup
    {
        public string GetAddress();
    }
}
#endif