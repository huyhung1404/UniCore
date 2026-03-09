#if ENABLE_UNI_PURCHASE
using System;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task<TimeSpan> GetSubscriptionTimeRemainingAsync(string productId)
        {
            if (_storeController == null || string.IsNullOrEmpty(productId)) 
                return TimeSpan.Zero;
            
            var product = _storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            if (product == null || product.definition.type != ProductType.Subscription) 
                return TimeSpan.Zero;

            var tcs = new TaskCompletionSource<TimeSpan>();

            Action<Entitlement> onCheckEntitlement = null;
            onCheckEntitlement = (entitlement) =>
            {
                if (entitlement.Product == null || entitlement.Product.definition.id != productId) 
                    return;

                _storeController.OnCheckEntitlement -= onCheckEntitlement;

                try
                {
                    if (entitlement.Status != EntitlementStatus.FullyEntitled && 
                        entitlement.Status != EntitlementStatus.EntitledButNotFinished)
                    {
                        tcs.TrySetResult(TimeSpan.Zero);
                        return;
                    }

                    var order = entitlement.Order;
                    var subInfo = order?.Info.PurchasedProductInfo.FirstOrDefault()?.subscriptionInfo;

                    if (subInfo == null)
                    {
                        tcs.TrySetResult(TimeSpan.Zero);
                        return;
                    }

                    if (subInfo.IsExpired() == Result.True)
                    {
                        tcs.TrySetResult(TimeSpan.Zero);
                        return;
                    }

                    var remaining = subInfo.GetExpireDate() - DateTime.UtcNow;
                    tcs.TrySetResult(remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UniPurchase] Failed to parse v5 subscription: {ex.Message}");
                    tcs.TrySetResult(TimeSpan.Zero);
                }
            };

            _storeController.OnCheckEntitlement += onCheckEntitlement;
            _storeController.CheckEntitlement(product);

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _storeController.OnCheckEntitlement -= onCheckEntitlement;
                Debug.LogWarning($"[UniPurchase] Subscription check timed out for {productId}.");
                return TimeSpan.Zero;
            }

            return await tcs.Task;
        }
        
        public async Task<bool> IsSubscriptionActiveAsync(string productId)
        {
            var remainingTime = await GetSubscriptionTimeRemainingAsync(productId);
            return remainingTime.TotalSeconds > 0;
        }
    }
}
#endif