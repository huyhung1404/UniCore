#if ENABLE_UNI_PURCHASE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniPurchase
{
    public sealed class PurchaseService : MonoBehaviour
    {
        private StoreController _storeController;
        private PurchaseValidator _validator;
        private SubscriptionHelper _subscriptionHelper;
        private PurchaseConfig _config;
        
        private bool _isInitialized;
        private bool _isInitializing;
        
        private bool _isNativeStoreActive;
        private int _activeTransactions; 
        
        private readonly Dictionary<string, PendingOrder> _unconfirmedOrders = new Dictionary<string, PendingOrder>();

        public bool IsProcessing => _activeTransactions > 0;
        public bool IsInitialized => _isInitialized;

        public async Task InitializeAsync(byte[] appleTangleData = null, byte[] googleTangleData = null)
        {
            if (_isInitialized || _isInitializing) return;
            
            _isInitializing = true;

            _config = Resources.Load<PurchaseConfig>(PurchaseConfig.k_FileName);
            if (_config == null)
            {
                PurchaseEventDispatcher.DispatchInitializeFailed("Missing PurchaseConfig in Resources.");
                _isInitializing = false;
                return;
            }

            if (!_config.IsEnabled)
            {
                Debug.LogWarning("[UniPurchase] Purchase System is disabled in settings.");
                _isInitializing = false;
                return;
            }
            
            _config.InitializeCache();
            _validator = new PurchaseValidator(appleTangleData, googleTangleData);
            _storeController = UnityIAPServices.StoreController();

            _storeController.OnProductsFetched += HandleProductsFetched;
            _storeController.OnProductsFetchFailed += HandleProductsFetchFailed;
            _storeController.OnPurchasePending += HandlePurchasePending;
            _storeController.OnPurchaseDeferred += HandlePurchaseDeferred;
            _storeController.OnPurchaseFailed += HandlePurchaseFailed;
            
            _storeController.OnPurchasesFetched += HandlePurchasesFetched;
            _storeController.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;

            try
            {
                await _storeController.Connect();

                var initialProducts = new List<ProductDefinition>();
                foreach (var product in _config.Products)
                {
                    initialProducts.Add(new ProductDefinition(product.ProductId, product.ProductType));
                }

                _storeController.FetchProducts(initialProducts);
            }
            catch (Exception ex)
            {
                _isInitializing = false;
                PurchaseEventDispatcher.DispatchInitializeFailed(ex.Message);
            }
        }

        public async void BuyProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;

            if (!_isInitialized)
            {
                Debug.LogWarning("[UniPurchase] Store not initialized. Attempting auto-recovery...");
                await InitializeAsync();

                if (!_isInitialized)
                {
                    PurchaseEventDispatcher.DispatchPurchaseFailed(productId, "Store is not initialized. Please check network.");
                    return;
                }
            }
            
            var targetProduct = _storeController.products.WithID(productId);
            if (targetProduct == null)
            {
                var error = $"Product ID {productId} not found in Store.";
                Debug.LogError($"[UniPurchase] {error}");
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error);
                return;
            }

            if (IsProcessing)
            {
                Debug.LogWarning("[UniPurchase] A transaction is already in progress.");
                return;
            }

            BeginTransactionFlow();
            _storeController.PurchaseProduct(targetProduct);
        }

        public async void RestorePurchases()
        {
            if (IsProcessing) return;

            if (!_isInitialized)
            {
                Debug.LogWarning("[UniPurchase] Store not initialized. Attempting auto-recovery...");
                await InitializeAsync();

                if (!_isInitialized)
                {
                    PurchaseEventDispatcher.DispatchPurchaseFailed("RESTORE", "Store is not initialized. Please check network.");
                    return;
                }
            }

            BeginTransactionFlow();
            _storeController.FetchPurchases(); 
        }
        
        public void ConfirmTransaction(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            if (_unconfirmedOrders.TryGetValue(transactionId, out var pendingOrder))
            {
                _storeController.ConfirmPurchase(pendingOrder);
                _unconfirmedOrders.Remove(transactionId);
                Debug.Log($"[UniPurchase] Transaction {transactionId} permanently confirmed.");
            }
        }

        public bool CheckIsSubscriptionActive(string productId)
        {
            if (!_isInitialized || _subscriptionHelper == null) return false;
            return _subscriptionHelper.IsSubscriptionActive(productId);
        }
        
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused && IsProcessing)
            {
                _isNativeStoreActive = true;
            }
            else if (!isPaused && _isNativeStoreActive)
            {
                _isNativeStoreActive = false;
                HandleNativeStoreResumeAsync();
            }
        }

        private async void HandleNativeStoreResumeAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));

            if (IsProcessing)
            {
                Debug.LogWarning("[UniPurchase] OS returned but no callback fired. Silently unblocking UI.");
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd();
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
            _subscriptionHelper = new SubscriptionHelper(_storeController);
            PurchaseEventDispatcher.DispatchInitializeSuccess();
        }

        private void HandleProductsFetchFailed(string reason)
        {
            _isInitializing = false;
            PurchaseEventDispatcher.DispatchInitializeFailed(reason);
        }

        private void HandlePurchasePending(PendingOrder pendingOrder)
        {
            var receipt = pendingOrder.Receipt;
            var productInfo = pendingOrder.Info;
            var productId = productInfo.ProductId;
            var transactionId = productInfo.TransactionID;

            if (!IsProcessing) BeginTransactionFlow();

            if (!_validator.IsReceiptValid(receipt))
            {
                var error = "Invalid Receipt Validation";
                PurchaseEventDispatcher.DispatchPurchaseFailed(productId, error);
                EndTransactionFlow();
                return;
            }

            _unconfirmedOrders[transactionId] = pendingOrder;

            PurchaseEventDispatcher.DispatchPurchaseSuccess(productId, transactionId); 
            EndTransactionFlow();
        }

        private void HandlePurchaseDeferred(Product product)
        {
            PurchaseEventDispatcher.DispatchTransactionDeferred();
            EndTransactionFlow();
        }

        private void HandlePurchaseFailed(Product product, string reason)
        {
            var productId = product != null ? product.definition.id : "Unknown";
            PurchaseEventDispatcher.DispatchPurchaseFailed(productId, reason);
            EndTransactionFlow();
        }
        
        private void HandlePurchasesFetched(Orders orders)
        {
            var hasPendingOrders = orders.Pending != null && orders.Pending.Count > 0;
            
            if (!hasPendingOrders)
            {
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd();
                Debug.Log("[UniPurchase] Restore completed. No pending orders found.");
                return;
            }

            Debug.Log($"[UniPurchase] Restore processing {orders.Pending.Count} orders.");

            _activeTransactions++;

            foreach (var pendingOrder in orders.Pending)
            {
                HandlePurchasePending(pendingOrder);
            }

            EndTransactionFlow(); 
        }

        private void HandlePurchasesFetchFailed(string reason)
        {
            if (IsProcessing) 
            {
                _activeTransactions = 0;
                PurchaseEventDispatcher.DispatchTransactionEnd();
            }
            
            Debug.LogError($"[UniPurchase] Restore failed: {reason}");
            PurchaseEventDispatcher.DispatchPurchaseFailed("RESTORE", reason);
        }

        private void OnDestroy()
        {
            if (_storeController == null) return;
            _storeController.OnProductsFetched -= HandleProductsFetched;
            _storeController.OnProductsFetchFailed -= HandleProductsFetchFailed;
            _storeController.OnPurchasePending -= HandlePurchasePending;
            _storeController.OnPurchaseDeferred -= HandlePurchaseDeferred;
            _storeController.OnPurchaseFailed -= HandlePurchaseFailed;
            _storeController.OnPurchasesFetched -= HandlePurchasesFetched;
            _storeController.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
        }
    }
}
#endif