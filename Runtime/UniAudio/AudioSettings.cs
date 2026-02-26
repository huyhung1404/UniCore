#if HAS_UNITASK && HAS_ADDRESSABLES
using UniCore.Attribute;
using UnityEngine;
using UnityEngine.Audio;

namespace UniCore.Audio
{
    public sealed class AudioSettings : ScriptableObject
    {
        [SerializeField] private AddressGroupType m_addressType = AddressGroupType.Static;
        [SerializeField] private string m_staticAddress = "Assets/AudioSystem";
        [SerializeField] private InterfaceReference<IAddressGroup> m_customAddress;
        [SerializeField] private AudioMixer m_outputMixer;
        [SerializeField] private SoundEmitter m_soundEmitterPrefab;
        [SerializeField] private int m_poolInitialSize = 16;

        public string GroupAddress => m_addressType == AddressGroupType.Static ? m_staticAddress : m_customAddress.Value.GetAddress();
        public SoundEmitter SoundEmitterPrefab => m_soundEmitterPrefab;
        public int PoolInitialSize => m_poolInitialSize;
        public AudioMixer OutputMixer => m_outputMixer;
    }
}
#endif