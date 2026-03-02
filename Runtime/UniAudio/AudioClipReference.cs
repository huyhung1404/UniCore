#if HAS_UNITASK && HAS_ADDRESSABLES
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UniCore.Audio
{
    [System.Serializable]
    public class AudioClipReference : AssetReferenceT<AudioClip>
    {
        private AsyncOperationHandle<AudioClip>? _handle;

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
            if (!_handle.HasValue || !_handle.Value.IsValid())
            {
                _handle = Addressables.LoadAssetAsync<AudioClip>(this);
            }

            var op = _handle.Value;

            if (!op.IsDone) await op.Task;

            if (op.Status == AsyncOperationStatus.Succeeded) return op.Result;

            Debug.LogError("[AudioClipReference] Load failed");
            return null;
        }

        public void Release()
        {
            if (_handle.HasValue && _handle.Value.IsValid())
            {
                Addressables.Release(_handle.Value);
                return;
            }

            _handle = null;
        }
    }
}
#endif