#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UniCore.Audio.Node
{
    [System.Serializable]
    public class AudioClipReference : AssetReferenceT<AudioClip>
    {
        private AsyncOperationHandle<AudioClip>? handle;

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

        public async UniTask<AudioClip> LoadAsync()
        {
            if (!handle.HasValue || !handle.Value.IsValid())
            {
                handle = Addressables.LoadAssetAsync<AudioClip>(this);
            }

            var op = handle.Value;

            if (!op.IsDone) await op.Task;

            if (op.Status == AsyncOperationStatus.Succeeded) return op.Result;

            Debug.LogError("[AudioClipReference] Load failed");
            return null;
        }

        public void Release()
        {
            if (handle.HasValue && handle.Value.IsValid())
            {
                Addressables.Release(handle.Value);
                return;
            }

            handle = null;
        }
    }

    public abstract class BaseAudioNode : ScriptableObject
    {
        public abstract UniTask<ClipData> GetClipData();
    }
}
#endif