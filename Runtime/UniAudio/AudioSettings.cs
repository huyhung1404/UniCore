#if HAS_UNITASK && HAS_ADDRESSABLES
using UniCore.Attribute;
using UnityEngine;

namespace UniCore.Audio
{
    public sealed class AudioSettings : ScriptableObject
    {
        [SerializeField] private AddressGroupType addressType = AddressGroupType.Static;
        [SerializeField] private string staticAddress = "Assets/AudioSystem";
        [SerializeField] private InterfaceReference<IAddressGroup> customAddress;
        [SerializeField] private SoundEmitter soundEmitterPrefab;

        public string GroupAddress => addressType == AddressGroupType.Static ? staticAddress : customAddress.Value.GetAddress();
        public SoundEmitter SoundEmitterPrefab => soundEmitterPrefab;
    }
}
#endif