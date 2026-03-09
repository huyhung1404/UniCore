#if ENABLE_UNI_PURCHASE
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniPurchase
{
    [Serializable]
    public class ProductData
    {
        [SerializeField] private string m_productId;
        [SerializeField] private ProductType m_productType;
        [SerializeField, Range(0, 1)] private float m_discountPercent;

        public string ProductId => m_productId;
        public ProductType ProductType => m_productType;
        public float DiscountPercent => m_discountPercent;

        public ProductData WithProductId(string id)
        {
            m_productId = id;
            return this;
        }

        public ProductData WithProductType(ProductType type)
        {
            m_productType = type;
            return this;
        }
    }

    public sealed class PurchaseConfig : ScriptableObject
    {
        public const string k_FileName = "UniPurcase_PurchaseConfigs_Runtime";

        [SerializeField] private List<ProductData> m_products = new List<ProductData>();
        [SerializeField] private bool m_isEnabled;

        private Dictionary<string, ProductData> _productDict;

        public IReadOnlyList<ProductData> Products => m_products;
        public bool IsEnabled => m_isEnabled;

        internal void SetUp(bool isEnabled, List<ProductData> products)
        {
            m_isEnabled = isEnabled;
            m_products = products;
        }

        public void InitializeCache()
        {
            if (_productDict != null) return;

            _productDict = new Dictionary<string, ProductData>();
            foreach (var product in m_products)
            {
                _productDict[product.ProductId] = product;
            }
        }

        public ProductData GetProductData(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return null;
            if (_productDict == null) InitializeCache();
            return _productDict.GetValueOrDefault(productId);
        }
    }
}
#endif