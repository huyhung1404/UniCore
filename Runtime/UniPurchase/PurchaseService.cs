#if ENABLE_UNI_PURCHASE
using System;
using System.Collections.Generic;
using UnityEngine;
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

        private BasePaymentChannel _channel;
        private PurchaseConfig _config;

        private bool _isInitialized;
        private bool _isInitializing;

        private bool _isNativeStoreActive;
        private int _activeTransactions;
        private string _currentCustomAttribute;

        private readonly Dictionary<string, string> _unconfirmedTransactions = new Dictionary<string, string>();
#if HAS_UNITASK
        private readonly Dictionary<string, UniTaskCompletionSource<PurchaseResult>> _purchaseCompletions = new Dictionary<string, UniTaskCompletionSource<PurchaseResult>>();
#else
        private readonly Dictionary<string, TaskCompletionSource<PurchaseResult>> _purchaseCompletions = new Dictionary<string, TaskCompletionSource<PurchaseResult>>();
#endif

        private static PurchaseLifecycleTracker s_lifecycleTracker;

        public static bool IsProcessing => Instance._activeTransactions > 0;
        public static bool IsInitialized => Instance._isInitialized;

        private PurchaseService()
        {
        }

        public static void SetConfig(PurchaseConfig config)
        {
            Instance._config = config;
#if UNITY_EDITOR
            EditorPurchaseConfig.SetUpConfig(Instance._config);
#endif
        }

        public static void SetChannel(BasePaymentChannel channel)
        {
            Instance._channel = channel;
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

            if (!inst._isInitialized || inst._channel == null) return new ProductPriceInfo("---", "---", false);

            if (!inst._channel.HasProduct(productId)) return new ProductPriceInfo("---", "---", false);

            var originalString = inst._channel.GetLocalizedPriceString(productId);
            if (!hasDiscount) return new ProductPriceInfo(originalString, originalString, false);

            var decimalPrice = inst._channel.GetLocalizedPrice(productId);
            var currencyCode = inst._channel.GetIsoCurrencyCode(productId);
            var finalDecimalPrice = decimalPrice * (1m - (decimal)discountPercent);

            var discountedString = $"{finalDecimalPrice:0.##} {currencyCode}";
            return new ProductPriceInfo(originalString, discountedString, true);
        }

#if HAS_UNITASK
        public static async UniTask InitializeAsync()
#else
        public static async Task InitializeAsync()
#endif
        {
            var inst = Instance;

            if (inst._isInitialized || inst._isInitializing) return;

            if (inst._channel == null)
            {
                var error = "No payment channel set. Call SetChannel() before InitializeAsync().";
                Debug.LogError($"[UniPurchase] {error}");
                PurchaseEventDispatcher.DispatchInitializeFailed(error);
                return;
            }

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
                s_lifecycleTracker = UnityEngine.Object.FindAnyObjectByType<PurchaseLifecycleTracker>();
                if (s_lifecycleTracker == null)
                {
                    var go = new GameObject("[UniPurchase] LifecycleTracker");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    s_lifecycleTracker = go.AddComponent<PurchaseLifecycleTracker>();
                }
            }

            inst._channel.OnInitialized += inst.HandleChannelInitialized;
            inst._channel.OnInitializeFailed += inst.HandleChannelInitializeFailed;
            inst._channel.OnPurchasePending += inst.HandleChannelPurchasePending;
            inst._channel.OnPurchaseDeferred += inst.HandleChannelPurchaseDeferred;
            inst._channel.OnPurchaseFailed += inst.HandleChannelPurchaseFailed;
            inst._channel.OnPurchasesRestored += inst.HandleChannelPurchasesRestored;
            inst._channel.OnPurchasesRestoreFailed += inst.HandleChannelPurchasesRestoreFailed;

            try
            {
                await inst._channel.InitializeAsync(inst._config.Products);
            }
            catch (Exception ex)
            {
                inst._isInitializing = false;
                PurchaseEventDispatcher.DispatchInitializeFailed(ex.Message);
            }
        }

#if HAS_UNITASK
        public static async UniTask<PurchaseResult> BuyProduct(string productId, string customAttribute = null)
#else
        public static async Task<PurchaseResult> BuyProduct(string productId, string customAttribute = null)
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
            inst._currentCustomAttribute = customAttribute;

            if (!inst._isInitialized)
            {
                if (inst._channel != null)
                {
                    Debug.LogWarning("[UniPurchase] Store not initialized. Attempting auto-recovery...");
                    await InitializeAsync();
                }

                if (!inst._isInitialized)
                {
                    var error = "Store is not initialized. Please check network connection.";
                    PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error, customAttribute);
                    return PurchaseResult.Failure(productId, error);
                }
            }

            if (!inst._channel.HasProduct(productId))
            {
                var error = $"Product ID {productId} not found in Store.";
                Debug.LogError($"[UniPurchase] {error}");
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error, customAttribute);
                return PurchaseResult.Failure(productId, error);
            }

#if HAS_UNITASK
            var tcs = new UniTaskCompletionSource<PurchaseResult>();
#else
            var tcs = new TaskCompletionSource<PurchaseResult>();
#endif
            inst._purchaseCompletions[productId] = tcs;

            inst.BeginTransactionFlow();
            inst._channel.Purchase(productId);

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
            inst._currentCustomAttribute = null;

            if (!inst._isInitialized)
            {
                if (inst._channel != null)
                {
                    Debug.LogWarning("[UniPurchase] Store not initialized. Attempting auto-recovery...");
                    await InitializeAsync();
                }

                if (!inst._isInitialized)
                {
                    PurchaseEventDispatcher.DispatchPurchaseFailed("RESTORE", "Store is not initialized. Please check network connection.", null);
                    return;
                }
            }

            inst.BeginTransactionFlow();
            inst._channel.RestorePurchases();
        }

        public static void ConfirmTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            var inst = Instance;
            if (inst._unconfirmedTransactions.TryGetValue(transactionId, out var productId))
            {
                inst._channel?.ConfirmPurchase(transactionId);
                inst._unconfirmedTransactions.Remove(transactionId);
                Debug.Log($"[UniPurchase] Transaction {transactionId} permanently confirmed.");
                inst.CompletePurchase(PurchaseResult.Success(productId, transactionId));
            }
        }

        public static void RejectTransaction(string transactionId, string reason)
        {
            if (string.IsNullOrEmpty(transactionId)) return;
            var inst = Instance;
            if (inst._unconfirmedTransactions.TryGetValue(transactionId, out var productId))
            {
                inst._unconfirmedTransactions.Remove(transactionId);
                Debug.LogWarning($"[UniPurchase] Transaction {transactionId} rejected: {reason}");
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, reason, inst._currentCustomAttribute);
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
            if (!inst._isInitialized || inst._channel == null) return false;
            return await inst._channel.IsSubscriptionActiveAsync(productId);
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

            if (inst._channel != null)
            {
                inst._channel.OnInitialized -= inst.HandleChannelInitialized;
                inst._channel.OnInitializeFailed -= inst.HandleChannelInitializeFailed;
                inst._channel.OnPurchasePending -= inst.HandleChannelPurchasePending;
                inst._channel.OnPurchaseDeferred -= inst.HandleChannelPurchaseDeferred;
                inst._channel.OnPurchaseFailed -= inst.HandleChannelPurchaseFailed;
                inst._channel.OnPurchasesRestored -= inst.HandleChannelPurchasesRestored;
                inst._channel.OnPurchasesRestoreFailed -= inst.HandleChannelPurchasesRestoreFailed;
                inst._channel.Dispose();
            }

            inst._isInitialized = false;
            inst._isInitializing = false;
            inst._activeTransactions = 0;
            inst._currentCustomAttribute = null;
            inst._unconfirmedTransactions.Clear();

            foreach (var kvp in inst._purchaseCompletions)
                kvp.Value.TrySetResult(PurchaseResult.Failure(kvp.Key, "Service disposed"));
            inst._purchaseCompletions.Clear();

            inst._channel = null;
            inst._config = null;

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
                    PurchaseEventDispatcher.DispatchTransactionEnd(_currentCustomAttribute);
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
            if (_activeTransactions == 1) PurchaseEventDispatcher.DispatchTransactionStart(_currentCustomAttribute);
        }

        private void EndTransactionFlow()
        {
            _activeTransactions--;
            if (_activeTransactions > 0) return;
            _activeTransactions = 0;
            PurchaseEventDispatcher.DispatchTransactionEnd(_currentCustomAttribute);
        }

        private void HandleChannelInitialized()
        {
            _isInitialized = true;
            _isInitializing = false;
            PurchaseEventDispatcher.DispatchInitializeSuccess();
        }

        private void HandleChannelInitializeFailed(string error)
        {
            _isInitializing = false;
            PurchaseEventDispatcher.DispatchInitializeFailed(error);
        }

        private void HandleChannelPurchasePending(string productId, string transactionId)
        {
            if (!IsProcessing) BeginTransactionFlow();
            _unconfirmedTransactions[transactionId] = productId;
            PurchaseEventDispatcher.DispatchPurchaseSuccess(productId, transactionId, _currentCustomAttribute);
            EndTransactionFlow();
        }

        private void HandleChannelPurchaseDeferred()
        {
            if (!IsProcessing) BeginTransactionFlow();
            PurchaseEventDispatcher.DispatchTransactionDeferred(_currentCustomAttribute);
            EndTransactionFlow();
        }

        private void HandleChannelPurchaseFailed(string productId, string reason)
        {
            if (!IsProcessing) BeginTransactionFlow();
            PurchaseEventDispatcher.DispatchPurchaseFailed(productId, reason, _currentCustomAttribute);
            CompletePurchase(PurchaseResult.Failure(productId, reason));
            EndTransactionFlow();
        }

        private void HandleChannelPurchasesRestored(IReadOnlyList<PurchaseRestoreResult> results)
        {
            if (results.Count == 0)
            {
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd(_currentCustomAttribute);
                Debug.Log("[UniPurchase] Restore completed. No pending orders found.");
                return;
            }

            Debug.Log($"[UniPurchase] Restore processing {results.Count} orders.");
            _activeTransactions += results.Count;

            foreach (var result in results)
            {
                if (result.IsValid)
                {
                    _unconfirmedTransactions[result.TransactionId] = result.ProductId;
                    PurchaseEventDispatcher.DispatchPurchaseSuccess(result.ProductId, result.TransactionId, _currentCustomAttribute);
                }
                else
                {
                    PurchaseEventDispatcher.DispatchPurchaseFailed(result.ProductId, result.FailureReason, _currentCustomAttribute);
                    CompletePurchase(PurchaseResult.Failure(result.ProductId, result.FailureReason));
                }

                EndTransactionFlow();
            }

            EndTransactionFlow();
        }

        private void HandleChannelPurchasesRestoreFailed(string reason)
        {
            Debug.LogError($"[UniPurchase] Restore failed: {reason}");
            PurchaseEventDispatcher.DispatchPurchaseFailed("RESTORE", reason, _currentCustomAttribute);

            if (IsProcessing)
            {
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd(_currentCustomAttribute);
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
