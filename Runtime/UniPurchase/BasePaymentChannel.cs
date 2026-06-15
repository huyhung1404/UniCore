#if ENABLE_UNI_PURCHASE
using System;
using System.Collections.Generic;
#if HAS_UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace UniPurchase
{
    public readonly struct PurchaseRestoreResult
    {
        public string ProductId { get; }
        public string TransactionId { get; }
        public bool IsValid { get; }
        public string FailureReason { get; }

        public PurchaseRestoreResult(string productId, string transactionId, bool isValid, string failureReason)
        {
            ProductId = productId;
            TransactionId = transactionId;
            IsValid = isValid;
            FailureReason = failureReason;
        }
    }

    public abstract class BasePaymentChannel
    {
        public event Action OnInitialized;
        public event Action<string> OnInitializeFailed;
        public event Action<string, string> OnPurchasePending;
        public event Action OnPurchaseDeferred;
        public event Action<string, string> OnPurchaseFailed;
        public event Action<IReadOnlyList<PurchaseRestoreResult>> OnPurchasesRestored;
        public event Action<string> OnPurchasesRestoreFailed;

#if HAS_UNITASK
        public abstract UniTask InitializeAsync(IReadOnlyList<ProductData> products);
#else
        public abstract Task InitializeAsync(IReadOnlyList<ProductData> products);
#endif
        public abstract void Purchase(string productId);
        public abstract void ConfirmPurchase(string transactionId);
        public abstract void RestorePurchases();
        public abstract void Dispose();

        public abstract bool HasProduct(string productId);
        public abstract string GetLocalizedPriceString(string productId);
        public abstract decimal GetLocalizedPrice(string productId);
        public abstract string GetIsoCurrencyCode(string productId);

#if HAS_UNITASK
        public abstract UniTask<bool> IsSubscriptionActiveAsync(string productId);
#else
        public abstract Task<bool> IsSubscriptionActiveAsync(string productId);
#endif

        protected void RaiseInitialized() => OnInitialized?.Invoke();
        protected void RaiseInitializeFailed(string error) => OnInitializeFailed?.Invoke(error);
        protected void RaisePurchasePending(string productId, string transactionId) => OnPurchasePending?.Invoke(productId, transactionId);
        protected void RaisePurchaseDeferred() => OnPurchaseDeferred?.Invoke();
        protected void RaisePurchaseFailed(string productId, string reason) => OnPurchaseFailed?.Invoke(productId, reason);
        protected void RaisePurchasesRestored(IReadOnlyList<PurchaseRestoreResult> results) => OnPurchasesRestored?.Invoke(results);
        protected void RaisePurchasesRestoreFailed(string reason) => OnPurchasesRestoreFailed?.Invoke(reason);
    }
}
#endif
