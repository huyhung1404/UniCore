#if ENABLE_UNI_PURCHASE
using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniPurchase
{
    public sealed class SubscriptionHelper
    {
        private StoreController _storeController;

        public SubscriptionHelper(StoreController controller)
        {
            _storeController = controller;
        }

        public TimeSpan GetSubscriptionTimeRemaining(string productId)
        {
            if (_storeController == null) return TimeSpan.Zero;
            if (string.IsNullOrEmpty(productId)) return TimeSpan.Zero;

            var product = _storeController.products.WithID(productId);
            
            if (product == null || !product.hasReceipt) return TimeSpan.Zero;
            if (product.definition.type != ProductType.Subscription) return TimeSpan.Zero;

            try
            {
                var introJson = product.receipt; 
                var manager = new SubscriptionManager(product, introJson);
                var info = manager.getSubscriptionInfo();

                var isExpired = info.isExpired() == Result.True;
                if (isExpired) return TimeSpan.Zero;

                var remaining = info.getExpireDate() - DateTime.UtcNow;
                return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UniPurchase] Failed to parse subscription: {ex.Message}");
                return TimeSpan.Zero;
            }
        }
        
        public bool IsSubscriptionActive(string productId)
        {
            var remainingTime = GetSubscriptionTimeRemaining(productId);
            return remainingTime.TotalSeconds > 0;
        }
    }
}
#endif