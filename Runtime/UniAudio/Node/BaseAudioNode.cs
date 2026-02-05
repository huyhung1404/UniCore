#if HAS_UNITASK && HAS_ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniCore.Audio.Node
{
    [System.Serializable]
    public class AudioClipReference : AssetReferenceT<AudioClip>
    {
        public AudioClipReference(string guid) : base(guid)
        {
        }

        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(AudioClip);
#else
            return false;
#endif
        }
    }

    public abstract class BaseAudioNode : ScriptableObject
    {
    }
}
#endif