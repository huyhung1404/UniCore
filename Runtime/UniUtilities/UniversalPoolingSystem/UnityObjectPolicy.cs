using UnityEngine;

namespace UniCore.Utilities.Pooling
{
    public class UnityObjectPolicy<T> : PoolPolicy<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _root;

        public UnityObjectPolicy(T prefab, Transform root = null)
        {
            this._prefab = prefab;
            this._root = root;
        }

        public override T Create()
        {
            var obj = Object.Instantiate(_prefab, _root);
            obj.gameObject.SetActive(false);
            return obj;
        }

        public override void OnGet(T obj)
        {
            obj.gameObject.SetActive(true);
            if (obj is IPoolable p) p.OnRent();
        }

        public override void OnRelease(T obj)
        {
            if (obj is IPoolable p) p.OnReturn();
            obj.gameObject.SetActive(false);
        }

        public override void OnDestroy(T obj)
        {
            Object.Destroy(obj.gameObject);
        }
    }
}