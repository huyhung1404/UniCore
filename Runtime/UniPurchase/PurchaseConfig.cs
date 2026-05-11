#if ENABLE_UNI_PURCHASE
using System.Collections.Generic;
using UnityEngine;

namespace UniPurchase
{
    public sealed class PurchaseConfig : ScriptableObject
    {
        public const string k_FileName = "UniPurcase_PurchaseConfigs_Runtime";

        [SerializeField] private List<ProductData> m_products = new List<ProductData>();
        [SerializeField] private bool m_isEnabled;

        public IReadOnlyList<ProductData> Products => m_products;
        public bool IsEnabled => m_isEnabled;

        internal void SetUp(bool isEnabled, List<ProductData> products)
        {
            m_isEnabled = isEnabled;
            m_products = products;
        }

        public ProductData GetProductData(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            foreach (var product in m_products)
            {
                if (product != null && product.ProductId == productId)
                    return product;
            }

            return null;
        }

        public T GetProductData<T>(string productId) where T : ProductData
        {
            return GetProductData(productId) as T;
        }
    }
}
#endif