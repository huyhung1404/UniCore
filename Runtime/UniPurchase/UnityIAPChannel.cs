#if ENABLE_UNI_PURCHASE
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
#if HAS_UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace UniPurchase
{
    public sealed class UnityIAPChannel : BasePaymentChannel
    {
        public event Action<string, string, PendingOrder> OnPurchasePendingWithOrder;

        private StoreController _storeController;
        private PurchaseValidator _validator;
        private SubscriptionHelper _subscriptionHelper;
        private readonly Func<byte[]> _googleTangleData;

        private readonly Dictionary<string, PendingOrder> _pendingOrders = new Dictionary<string, PendingOrder>();

        public UnityIAPChannel(Func<byte[]> googleTangleData)
        {
            _googleTangleData = googleTangleData;
        }

#if HAS_UNITASK
        public override async UniTask InitializeAsync(IReadOnlyList<ProductData> products)
#else
        public override async Task InitializeAsync(IReadOnlyList<ProductData> products)
#endif
        {
            _validator = new PurchaseValidator(_googleTangleData);
            _storeController = UnityIAPServices.StoreController();

            _storeController.OnProductsFetched += HandleProductsFetched;
            _storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
            _storeController.OnPurchasePending += HandlePurchasePending;
            _storeController.OnPurchaseDeferred += HandlePurchaseDeferred;
            _storeController.OnPurchaseFailed += HandlePurchaseFailed;
            _storeController.OnPurchasesFetched += HandlePurchasesFetched;
            _storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;

            await _storeController.Connect();

            var definitions = new List<ProductDefinition>();
            foreach (var product in products)
            {
                definitions.Add(new ProductDefinition(product.ProductId, ToUnityProductType(product.ProductType)));
            }

            _storeController.FetchProducts(definitions);
        }

        public override void Purchase(string productId)
        {
            var targetProduct = _storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            if (targetProduct != null)
            {
                _storeController.PurchaseProduct(targetProduct);
            }
        }

        public override void ConfirmPurchase(string transactionId)
        {
            if (_pendingOrders.TryGetValue(transactionId, out var pendingOrder))
            {
                _storeController?.ConfirmPurchase(pendingOrder);
                _pendingOrders.Remove(transactionId);
            }
        }

        public override void RestorePurchases()
        {
            _storeController.FetchPurchases();
        }

        public override void Dispose()
        {
            if (_storeController != null)
            {
                _storeController.OnProductsFetched -= HandleProductsFetched;
                _storeController.OnProductsFetchFailed -= HandleProductsFetchFailed;
                _storeController.OnPurchasePending -= HandlePurchasePending;
                _storeController.OnPurchaseDeferred -= HandlePurchaseDeferred;
                _storeController.OnPurchaseFailed -= HandlePurchaseFailed;
                _storeController.OnPurchasesFetched -= HandlePurchasesFetched;
                _storeController.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
            }

            _pendingOrders.Clear();
            _storeController = null;
            _validator = null;
            _subscriptionHelper = null;
        }

        public override bool HasProduct(string productId)
        {
            return _storeController?.GetProducts().Any(p => p.definition.id == productId) ?? false;
        }

        public override string GetLocalizedPriceString(string productId)
        {
            var product = FindProduct(productId);
            return product?.metadata.localizedPriceString;
        }

        public override decimal GetLocalizedPrice(string productId)
        {
            var product = FindProduct(productId);
            return product?.metadata.localizedPrice ?? 0m;
        }

        public override string GetIsoCurrencyCode(string productId)
        {
            var product = FindProduct(productId);
            return product?.metadata.isoCurrencyCode;
        }

#if HAS_UNITASK
        public override UniTask<bool> IsSubscriptionActiveAsync(string productId)
#else
        public override Task<bool> IsSubscriptionActiveAsync(string productId)
#endif
        {
            if (_subscriptionHelper == null)
            {
#if HAS_UNITASK
                return UniTask.FromResult(false);
#else
                return Task.FromResult(false);
#endif
            }

            return _subscriptionHelper.IsSubscriptionActiveAsync(productId);
        }

        private static ProductType ToUnityProductType(PurchaseProductType type)
        {
            switch (type)
            {
                case PurchaseProductType.Consumable: return ProductType.Consumable;
                case PurchaseProductType.NonConsumable: return ProductType.NonConsumable;
                case PurchaseProductType.Subscription: return ProductType.Subscription;
                default: return ProductType.Consumable;
            }
        }

        private Product FindProduct(string productId)
        {
            return _storeController?.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        }

        private void HandleProductsFetched(IReadOnlyList<Product> products)
        {
            _subscriptionHelper = new SubscriptionHelper(_storeController);
            RaiseInitialized();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failed)
        {
            RaiseInitializeFailed(failed.FailureReason);
        }

        private void HandlePurchasePending(PendingOrder pendingOrder)
        {
            var receipt = pendingOrder.Info.Receipt;
            var transactionId = pendingOrder.Info.TransactionID;
            var firstItem = pendingOrder.CartOrdered.Items().FirstOrDefault();
            var productId = firstItem != null ? firstItem.Product.definition.id : "Unknown";

            if (!_validator.IsReceiptValid(receipt))
            {
                RaisePurchaseFailed(productId, "Invalid Receipt Validation");
                return;
            }

            _pendingOrders[transactionId] = pendingOrder;
            RaisePurchasePending(productId, transactionId);
            OnPurchasePendingWithOrder?.Invoke(productId, transactionId, pendingOrder);
        }

        private void HandlePurchaseDeferred(DeferredOrder deferredOrder)
        {
            RaisePurchaseDeferred();
        }

        private void HandlePurchaseFailed(FailedOrder failedOrder)
        {
            var firstItem = failedOrder.CartOrdered.Items().FirstOrDefault();
            var productId = firstItem != null ? firstItem.Product.definition.id : "Unknown";
            var reason = string.IsNullOrEmpty(failedOrder.Details) ? failedOrder.FailureReason.ToString() : failedOrder.Details;
            RaisePurchaseFailed(productId, reason);
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            var pendingOrders = orders.PendingOrders;
            if (pendingOrders == null || pendingOrders.Count == 0)
            {
                RaisePurchasesRestored(Array.Empty<PurchaseRestoreResult>());
                return;
            }

            var results = new List<PurchaseRestoreResult>(pendingOrders.Count);
            foreach (var pendingOrder in pendingOrders)
            {
                var receipt = pendingOrder.Info.Receipt;
                var transactionId = pendingOrder.Info.TransactionID;
                var firstItem = pendingOrder.CartOrdered.Items().FirstOrDefault();
                var productId = firstItem != null ? firstItem.Product.definition.id : "Unknown";

                if (_validator.IsReceiptValid(receipt))
                {
                    _pendingOrders[transactionId] = pendingOrder;
                    results.Add(new PurchaseRestoreResult(productId, transactionId, true, null));
                }
                else
                {
                    results.Add(new PurchaseRestoreResult(productId, transactionId, false, "Invalid Receipt Validation"));
                }
            }

            RaisePurchasesRestored(results);
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failureDescription)
        {
            var reason = string.IsNullOrEmpty(failureDescription.Message)
                ? failureDescription.FailureReason.ToString()
                : failureDescription.Message;
            RaisePurchasesRestoreFailed(reason);
        }
    }
}
#endif
