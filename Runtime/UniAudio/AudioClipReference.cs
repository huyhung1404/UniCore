#if HAS_UNITASK && HAS_ADDRESSABLES
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UniCore.Audio
{
    [Serializable]
    public class AudioClipReference : AssetReferenceT<AudioClip>
    {
        private AsyncOperationHandle<AudioClip>? _handle;
        private int _refCount;
        private int _releaseToken;

#if UNITY_EDITOR
        public static readonly HashSet<AudioClipReference> ActiveRefs = new HashSet<AudioClipReference>();
        public AudioClip GetLoadedClip() => _handle.HasValue && _handle.Value.IsValid() && _handle.Value.IsDone ? _handle.Value.Result : null;
        public bool IsDelayingRelease => _refCount == 0 && _handle.HasValue && _handle.Value.IsValid();
#endif

        public AudioClipReference(string guid) : base(guid) { }

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
            _refCount++;
            _releaseToken++; 

#if UNITY_EDITOR
            ActiveRefs.Add(this);
#endif

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

        public void ReleaseUsage(float delay)
        {
            _refCount--;
            if (_refCount <= 0)
            {
                _refCount = 0;
                var currentToken = ++_releaseToken;
                
                if (delay <= 0f)
                {
                    ReleaseImmediately(currentToken);
                }
                else
                {
                    ReleaseAfterDelay(currentToken, delay).Forget();
                }
            }
        }

        private void ReleaseImmediately(int token)
        {
            if (token != _releaseToken || _refCount > 0 || !_handle.HasValue || !_handle.Value.IsValid()) return;
            
            Addressables.Release(_handle.Value);
            _handle = null;

#if UNITY_EDITOR
            ActiveRefs.Remove(this);
#endif
        }

        private async UniTaskVoid ReleaseAfterDelay(int token, float delay)
        {
            await UniTask.WaitForSeconds(delay);
            ReleaseImmediately(token);
        }
    }
}
#endif