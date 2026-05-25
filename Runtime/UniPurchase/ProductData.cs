#if ENABLE_UNI_PURCHASE
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniPurchase
{
    public struct ProductPriceInfo
    {
        public string OriginalPrice { get; }
        public string DiscountedPrice { get; }
        public bool HasDiscount { get; }

        public string Price => HasDiscount ? DiscountedPrice : OriginalPrice;

        internal ProductPriceInfo(string originalPrice, string discountedPrice, bool hasDiscount)
        {
            OriginalPrice = originalPrice;
            DiscountedPrice = discountedPrice;
            HasDiscount = hasDiscount;
        }
    }

    public class ProductData : ScriptableObject
    {
        [SerializeField] private string m_productId;
        [SerializeField] private ProductType m_productType;
        [SerializeField, Range(0, 1)] private float m_discountPercent;
        [SerializeField] private float m_price;

        public string ProductId => m_productId;
        public ProductType ProductType => m_productType;
        public float DiscountPercent => m_discountPercent;
        public float Price => m_price;

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

        public ProductData WithPrice(float price)
        {
            m_price = price;
            return this;
        }
    }
}
#endif