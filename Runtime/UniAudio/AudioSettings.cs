#if HAS_UNITASK && HAS_ADDRESSABLES
using UniCore.Attribute;
using UnityEngine;
using UnityEngine.Audio;

namespace UniCore.Audio
{
    public sealed class AudioSettings : ScriptableObject
    {
        [SerializeField] private AddressGroupType addressType = AddressGroupType.Static;
        [SerializeField] private string staticAddress = "Assets/AudioSystem";
        [SerializeField] private InterfaceReference<IAddressGroup> customAddress;
        [SerializeField] private AudioMixer outputMixer;
        [SerializeField] private SoundEmitter soundEmitterPrefab;
        [SerializeField] private int poolInitialSize = 16;

        public string GroupAddress => addressType == AddressGroupType.Static ? staticAddress : customAddress.Value.GetAddress();
        public SoundEmitter SoundEmitterPrefab => soundEmitterPrefab;
        public int PoolInitialSize => poolInitialSize;
        public AudioMixer OutputMixer => outputMixer;
    }
}
#endif