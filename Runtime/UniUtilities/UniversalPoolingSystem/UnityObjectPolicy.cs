using UnityEngine;

namespace UniCore.Utilities.Pooling
{
    public class UnityObjectPolicy<T> : PoolPolicy<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform root;

        public UnityObjectPolicy(T prefab, Transform root = null)
        {
            this.prefab = prefab;
            this.root = root;
        }

        public override T Create()
        {
            var obj = Object.Instantiate(prefab, root);
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