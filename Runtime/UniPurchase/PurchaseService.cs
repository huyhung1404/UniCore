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
#if HAS_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif

namespace UniPurchase
{
    public readonly struct PurchaseResult
    {
        public bool IsSuccess { get; }
        public string ProductId { get; }
        public string TransactionId { get; }
        public string FailureReason { get; }

        internal PurchaseResult(bool isSuccess, string productId, string transactionId, string failureReason)
        {
            IsSuccess = isSuccess;
            ProductId = productId;
            TransactionId = transactionId;
            FailureReason = failureReason;
        }

        public static PurchaseResult Success(string productId, string transactionId) =>
            new PurchaseResult(true, productId, transactionId, null);

        public static PurchaseResult Failure(string productId, string reason) =>
            new PurchaseResult(false, productId, null, reason);
    }

    public sealed class PurchaseService
    {
        private static PurchaseService s_instance;
        private static PurchaseService Instance => s_instance ??= new PurchaseService();

        private StoreController _storeController;
        private PurchaseValidator _validator;
        private SubscriptionHelper _subscriptionHelper;
        private PurchaseConfig _config;

        private bool _isInitialized;
        private bool _isInitializing;

        private bool _isNativeStoreActive;
        private int _activeTransactions;

        private readonly Dictionary<string, PendingOrder> _unconfirmedOrders = new Dictionary<string, PendingOrder>();
#if HAS_UNITASK
        private readonly Dictionary<string, UniTaskCompletionSource<PurchaseResult>> _purchaseCompletions = new Dictionary<string, UniTaskCompletionSource<PurchaseResult>>();
#else
        private readonly Dictionary<string, TaskCompletionSource<PurchaseResult>> _purchaseCompletions = new Dictionary<string, TaskCompletionSource<PurchaseResult>>();
#endif

        private static PurchaseLifecycleTracker s_lifecycleTracker;

        private MonoBehaviour _cachedLifecycleHost;
        private Func<byte[]> _cachedAppleTangle;
        private Func<byte[]> _cachedGoogleTangle;
        private bool _hasCachedDependencies;

        public static bool IsProcessing => Instance._activeTransactions > 0;
        public static bool IsInitialized => Instance._isInitialized;

        private PurchaseService()
        {
        }

        public static void SetConfig(PurchaseConfig config)
        {
            Instance._config = config;
        }

        public static T GetProductData<T>(string productId) where T : ProductData
        {
            return Instance._config?.GetProductData<T>(productId);
        }

        public static ProductPriceInfo GetPriceInfo(ProductData productData)
        {
            return GetPriceInfo(productData.ProductId);
        }
        
        public static ProductPriceInfo GetPriceInfo(string productId)
        {
            var inst = Instance;
            if (string.IsNullOrEmpty(productId)) return new ProductPriceInfo("$0.00", "$0.00", false);

            var productData = inst._config?.GetProductData(productId);
            var discountPercent = productData != null ? productData.DiscountPercent : 0f;
            var hasDiscount = discountPercent > 0f;

#if UNITY_EDITOR
            if (productData != null)
            {
                var simPrice = productData.Price;
                var simOriginal = $"${simPrice:0.00}";
                if (!hasDiscount) return new ProductPriceInfo(simOriginal, simOriginal, false);
                var simDiscounted = simPrice * (1f - discountPercent);
                return new ProductPriceInfo(simOriginal, $"${simDiscounted:0.00}", true);
            }
#endif

            if (!inst._isInitialized || inst._storeController == null) return new ProductPriceInfo("---", "---", false);

            var targetProduct = inst._storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            if (targetProduct == null) return new ProductPriceInfo("---", "---", false);

            var originalString = targetProduct.metadata.localizedPriceString;
            if (!hasDiscount) return new ProductPriceInfo(originalString, originalString, false);

            var decimalPrice = targetProduct.metadata.localizedPrice;
            var currencyCode = targetProduct.metadata.isoCurrencyCode;
            var finalDecimalPrice = decimalPrice * (1m - (decimal)discountPercent);

            var discountedString = $"{finalDecimalPrice:0.##} {currencyCode}";
            return new ProductPriceInfo(originalString, discountedString, true);
        }

#if HAS_UNITASK
        public static async UniTask InitializeAsync(MonoBehaviour lifecycleHost, Func<byte[]> googleTangleData, Func<byte[]> appleTangleData)
#else
        public static async Task InitializeAsync(MonoBehaviour lifecycleHost, Func<byte[]> googleTangleData, Func<byte[]> appleTangleData)
#endif
        {
            if (lifecycleHost == null)
            {
                var error = "Lifecycle Host (MonoBehaviour) cannot be null.";
                Debug.LogError($"[UniPurchase] {error}");
                PurchaseEventDispatcher.DispatchInitializeFailed(error);
                return;
            }

            if (googleTangleData == null || appleTangleData == null)
            {
                var error = "Tangle Data cannot be null for Receipt Validation.";
                Debug.LogError($"[UniPurchase] {error}");
                PurchaseEventDispatcher.DispatchInitializeFailed(error);
                return;
            }

            var inst = Instance;

            inst._cachedLifecycleHost = lifecycleHost;
            inst._cachedAppleTangle = appleTangleData;
            inst._cachedGoogleTangle = googleTangleData;
            inst._hasCachedDependencies = true;

            if (inst._isInitialized || inst._isInitializing) return;
            inst._isInitializing = true;

#if UNITY_EDITOR
            if (inst._config == null)
            {
                inst._config = EditorPurchaseConfig.CreateRuntimeInstance();
            }
#else
            if (inst._config == null)
            {
                inst._config = Resources.Load<PurchaseConfig>(PurchaseConfig.k_FileName);
            }
#if HAS_ADDRESSABLES
            if (inst._config == null)
            {
                var handle = Addressables.LoadAssetAsync<PurchaseConfig>(PurchaseConfig.k_FileName);
                inst._config = await handle.Task;
            }
#endif
#endif

            if (inst._config == null)
            {
                PurchaseEventDispatcher.DispatchInitializeFailed("Missing PurchaseConfig.");
                inst._isInitializing = false;
                return;
            }

            if (!inst._config.IsEnabled)
            {
                var error = "Purchase System is disabled in settings.";
                Debug.Log($"[UniPurchase] {error}");
                inst._isInitializing = false;
                PurchaseEventDispatcher.DispatchInitializeFailed(error);
                return;
            }

            if (s_lifecycleTracker == null)
            {
                s_lifecycleTracker = lifecycleHost.gameObject.GetComponent<PurchaseLifecycleTracker>();
                if (s_lifecycleTracker == null)
                {
                    s_lifecycleTracker = lifecycleHost.gameObject.AddComponent<PurchaseLifecycleTracker>();
                }
            }

            inst._validator = new PurchaseValidator(appleTangleData, googleTangleData);
            inst._storeController = UnityIAPServices.StoreController();

            inst._storeController.OnProductsFetched += inst.HandleProductsFetched;
            inst._storeController.OnProductsFetchFailed += inst.HandleProductsFetchFailed;
            inst._storeController.OnPurchasePending += inst.HandlePurchasePending;
            inst._storeController.OnPurchaseDeferred += inst.HandlePurchaseDeferred;
            inst._storeController.OnPurchaseFailed += inst.HandlePurchaseFailed;
            inst._storeController.OnPurchasesFetched += inst.HandlePurchasesFetched;
            inst._storeController.OnPurchasesFetchFailed += inst.HandlePurchasesFetchFailed;

            try
            {
                await inst._storeController.Connect();

                var initialProducts = new List<ProductDefinition>();
                foreach (var product in inst._config.Products)
                {
                    initialProducts.Add(new ProductDefinition(product.ProductId, product.ProductType));
                }

                inst._storeController.FetchProducts(initialProducts);
            }
            catch (Exception ex)
            {
                inst._isInitializing = false;
                PurchaseEventDispatcher.DispatchInitializeFailed(ex.Message);
            }
        }

#if HAS_UNITASK
        public static async UniTask<PurchaseResult> BuyProduct(string productId)
#else
        public static async Task<PurchaseResult> BuyProduct(string productId)
#endif
        {
            if (string.IsNullOrEmpty(productId))
                return PurchaseResult.Failure(productId, "Product ID is null or empty.");

            if (IsProcessing)
            {
                Debug.LogWarning("[UniPurchase] A transaction is already in progress.");
                return PurchaseResult.Failure(productId, "A transaction is already in progress.");
            }

            var inst = Instance;
            if (!inst._isInitialized)
            {
                if (inst._hasCachedDependencies)
                {
                    Debug.LogWarning("[UniPurchase] Store not initialized. Attempting auto-recovery using cached dependencies...");
                    await InitializeAsync(inst._cachedLifecycleHost, inst._cachedGoogleTangle, inst._cachedAppleTangle);
                }

                if (!inst._isInitialized)
                {
                    var error = "Store is not initialized. Please check network connection.";
                    PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error);
                    return PurchaseResult.Failure(productId, error);
                }
            }

            var targetProduct = inst._storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            if (targetProduct == null)
            {
                var error = $"Product ID {productId} not found in Store.";
                Debug.LogError($"[UniPurchase] {error}");
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error);
                return PurchaseResult.Failure(productId, error);
            }

#if HAS_UNITASK
            var tcs = new UniTaskCompletionSource<PurchaseResult>();
#else
            var tcs = new TaskCompletionSource<PurchaseResult>();
#endif
            inst._purchaseCompletions[productId] = tcs;

            inst.BeginTransactionFlow();
            inst._storeController.PurchaseProduct(targetProduct);

            return await tcs.Task;
        }

#if HAS_UNITASK
        public static async UniTask RestorePurchases()
#else
        public static async Task RestorePurchases()
#endif
        {
            if (IsProcessing) return;

            var inst = Instance;
            if (!inst._isInitialized)
            {
                if (inst._hasCachedDependencies)
                {
                    Debug.LogWarning("[UniPurchase] Store not initialized. Attempting auto-recovery using cached dependencies...");
                    await InitializeAsync(inst._cachedLifecycleHost, inst._cachedGoogleTangle, inst._cachedAppleTangle);
                }

                if (!inst._isInitialized)
                {
                    PurchaseEventDispatcher.DispatchPurchaseFailed("RESTORE", "Store is not initialized. Please check network connection.");
                    return;
                }
            }

            inst.BeginTransactionFlow();
            inst._storeController.FetchPurchases();
        }

        public static void ConfirmTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            var inst = Instance;
            if (inst._unconfirmedOrders.TryGetValue(transactionId, out var pendingOrder))
            {
                inst._storeController?.ConfirmPurchase(pendingOrder);
                inst._unconfirmedOrders.Remove(transactionId);
                Debug.Log($"[UniPurchase] Transaction {transactionId} permanently confirmed.");

                var firstItem = pendingOrder.CartOrdered.Items().FirstOrDefault();
                var productId = firstItem != null ? firstItem.Product.definition.id : null;
                inst.CompletePurchase(PurchaseResult.Success(productId, transactionId));
            }
        }

        public static void RejectTransaction(string transactionId, string reason)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            var inst = Instance;
            if (inst._unconfirmedOrders.TryGetValue(transactionId, out var pendingOrder))
            {
                inst._unconfirmedOrders.Remove(transactionId);

                var firstItem = pendingOrder.CartOrdered.Items().FirstOrDefault();
                var productId = firstItem != null ? firstItem.Product.definition.id : null;
                Debug.LogWarning($"[UniPurchase] Transaction {transactionId} rejected: {reason}");
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, reason);
                inst.CompletePurchase(PurchaseResult.Failure(productId, reason));
            }
        }

#if HAS_UNITASK
        public static async UniTask<bool> CheckIsSubscriptionActiveAsync(string productId)
#else
        public static async Task<bool> CheckIsSubscriptionActiveAsync(string productId)
#endif
        {
            var inst = Instance;
            if (!inst._isInitialized || inst._subscriptionHelper == null) return false;
            return await inst._subscriptionHelper.IsSubscriptionActiveAsync(productId);
        }

        public static void OnApplicationPause(bool isPaused)
        {
#if UNITY_IOS || UNITY_EDITOR
            var inst = Instance;
            if (isPaused && IsProcessing) inst._isNativeStoreActive = true;
            else if (!isPaused && inst._isNativeStoreActive)
            {
                inst._isNativeStoreActive = false;
                _ = inst.HandleNativeStoreResumeAsync();
            }
#endif
        }

        public static void OnApplicationFocus(bool hasFocus)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var inst = Instance;
            if (!hasFocus && IsProcessing) inst._isNativeStoreActive = true;
            else if (hasFocus && inst._isNativeStoreActive)
            {
                inst._isNativeStoreActive = false;
                _ = inst.HandleNativeStoreResumeAsync();
            }
#endif
        }

        internal static void OnLifecycleTrackerDestroyed(PurchaseLifecycleTracker tracker)
        {
            if (s_lifecycleTracker == tracker) Dispose();
        }

        public static void Dispose()
        {
            var inst = s_instance;
            if (inst == null) return;

            if (inst._storeController != null)
            {
                inst._storeController.OnProductsFetched -= inst.HandleProductsFetched;
                inst._storeController.OnProductsFetchFailed -= inst.HandleProductsFetchFailed;
                inst._storeController.OnPurchasePending -= inst.HandlePurchasePending;
                inst._storeController.OnPurchaseDeferred -= inst.HandlePurchaseDeferred;
                inst._storeController.OnPurchaseFailed -= inst.HandlePurchaseFailed;
                inst._storeController.OnPurchasesFetched -= inst.HandlePurchasesFetched;
                inst._storeController.OnPurchasesFetchFailed -= inst.HandlePurchasesFetchFailed;
            }

            inst._isInitialized = false;
            inst._isInitializing = false;
            inst._activeTransactions = 0;
            inst._unconfirmedOrders.Clear();

            foreach (var kvp in inst._purchaseCompletions)
                kvp.Value.TrySetResult(PurchaseResult.Failure(kvp.Key, "Service disposed"));
            inst._purchaseCompletions.Clear();

            inst._storeController = null;
            inst._validator = null;
            inst._subscriptionHelper = null;
            inst._config = null;
            inst._hasCachedDependencies = false;

            if (s_lifecycleTracker != null)
            {
                UnityEngine.Object.Destroy(s_lifecycleTracker);
                s_lifecycleTracker = null;
            }

            s_instance = null;
        }

#if HAS_UNITASK
        private async UniTask HandleNativeStoreResumeAsync()
#else
        private async Task HandleNativeStoreResumeAsync()
#endif
        {
            try
            {
#if HAS_UNITASK
                await UniTask.Delay(TimeSpan.FromSeconds(3));
#else
                await Task.Delay(TimeSpan.FromSeconds(3));
#endif
                if (IsProcessing)
                {
                    Debug.LogWarning("[UniPurchase] OS returned but no callback fired. Silently unblocking UI.");
                    _activeTransactions = 0;
                    PurchaseEventDispatcher.DispatchTransactionEnd();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniPurchase] Native Resume handler interrupted: {ex.Message}");
            }
        }

        private void CompletePurchase(PurchaseResult result)
        {
            if (result.ProductId != null && _purchaseCompletions.TryGetValue(result.ProductId, out var tcs))
            {
                _purchaseCompletions.Remove(result.ProductId);
                tcs.TrySetResult(result);
            }
        }

        private void BeginTransactionFlow()
        {
            _activeTransactions++;
            if (_activeTransactions == 1) PurchaseEventDispatcher.DispatchTransactionStart();
        }

        private void EndTransactionFlow()
        {
            _activeTransactions--;
            if (_activeTransactions > 0) return;
            _activeTransactions = 0;
            PurchaseEventDispatcher.DispatchTransactionEnd();
        }

        private void HandleProductsFetched(IReadOnlyList<Product> products)
        {
            _isInitialized = true;
            _isInitializing = false;
            _cachedAppleTangle = null;
            _cachedGoogleTangle = null;
            _subscriptionHelper = new SubscriptionHelper(_storeController);
            PurchaseEventDispatcher.DispatchInitializeSuccess();
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failed)
        {
            _isInitializing = false;
            PurchaseEventDispatcher.DispatchInitializeFailed(failed.FailureReason);
        }

        private void HandlePurchasePending(PendingOrder pendingOrder)
        {
            var receipt = pendingOrder.Info.Receipt;
            var transactionId = pendingOrder.Info.TransactionID;
            var firstItem = pendingOrder.CartOrdered.Items().FirstOrDefault();
            var productId = firstItem != null ? firstItem.Product.definition.id : "Unknown";

            if (!IsProcessing) BeginTransactionFlow();

            if (!_validator.IsReceiptValid(receipt))
            {
                var error = "Invalid Receipt Validation";
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error);
                CompletePurchase(PurchaseResult.Failure(productId, error));
                EndTransactionFlow();
                return;
            }

            _unconfirmedOrders[transactionId] = pendingOrder;
            PurchaseEventDispatcher.DispatchPurchaseSuccess(productId, transactionId, pendingOrder);
            EndTransactionFlow();
        }

        private void HandlePurchaseDeferred(DeferredOrder deferredOrder)
        {
            if (!IsProcessing) BeginTransactionFlow();
            PurchaseEventDispatcher.DispatchTransactionDeferred();
            EndTransactionFlow();
        }

        private void HandlePurchaseFailed(FailedOrder failedOrder)
        {
            if (!IsProcessing) BeginTransactionFlow();
            var firstItem = failedOrder.CartOrdered.Items().FirstOrDefault();
            var productId = firstItem != null ? firstItem.Product.definition.id : "Unknown";
            var reason = string.IsNullOrEmpty(failedOrder.Details) ? failedOrder.FailureReason.ToString() : failedOrder.Details;
            PurchaseEventDispatcher.DispatchPurchaseFailed(productId, reason);
            CompletePurchase(PurchaseResult.Failure(productId, reason));
            EndTransactionFlow();
        }

        private void HandlePurchasesFetched(Orders orders)
        {
            var pendingOrders = orders.PendingOrders;
            var hasPendingOrders = pendingOrders != null && pendingOrders.Count > 0;

            if (!hasPendingOrders)
            {
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd();
                Debug.Log("[UniPurchase] Restore completed. No pending orders found.");
                return;
            }

            Debug.Log($"[UniPurchase] Restore processing {pendingOrders.Count} orders.");
            _activeTransactions += pendingOrders.Count;
            foreach (var pendingOrder in pendingOrders)
            {
                HandlePurchasePending(pendingOrder);
            }

            EndTransactionFlow();
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failureDescription)
        {
            var reason = string.IsNullOrEmpty(failureDescription.Message) ? failureDescription.FailureReason.ToString() : failureDescription.Message;
            Debug.LogError($"[UniPurchase] Restore failed: {reason}");
            PurchaseEventDispatcher.DispatchPurchaseFailed("RESTORE", reason);

            if (IsProcessing)
            {
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd();
            }
        }
    }

    public class PurchaseLifecycleTracker : MonoBehaviour
    {
        private void OnApplicationPause(bool isPaused) => PurchaseService.OnApplicationPause(isPaused);
        private void OnApplicationFocus(bool hasFocus) => PurchaseService.OnApplicationFocus(hasFocus);
        private void OnDestroy() => PurchaseService.OnLifecycleTrackerDestroyed(this);
    }
}
#endif