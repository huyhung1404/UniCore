#if ENABLE_UNI_PURCHASE
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniPurchase
{
    public delegate void InitializeSuccessDelegate();
    public delegate void InitializeFailedDelegate(string error);
    public delegate void TransactionDelegate(string customAttribute);
    public delegate void PurchaseSuccessDelegate(string productId, string transactionId, PendingOrder pendingOrder, string customAttribute);
    public delegate void PurchaseFailedDelegate(string productId, string reason, string customAttribute);

    public static class PurchaseEventDispatcher
    {
        public static event InitializeSuccessDelegate OnInitializeSuccess;
        public static event InitializeFailedDelegate OnInitializeFailed;

        public static event TransactionDelegate OnTransactionStart;
        public static event TransactionDelegate OnTransactionEnd;
        public static event TransactionDelegate OnTransactionDeferred;

        public static event PurchaseSuccessDelegate OnPurchaseSuccess;
        public static event PurchaseFailedDelegate OnPurchaseFailed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetEvents()
        {
            OnInitializeSuccess = null;
            OnInitializeFailed = null;
            OnTransactionStart = null;
            OnTransactionEnd = null;
            OnTransactionDeferred = null;
            OnPurchaseSuccess = null;
            OnPurchaseFailed = null;
        }

        public static void DispatchInitializeSuccess() => OnInitializeSuccess?.Invoke();
        public static void DispatchInitializeFailed(string error) => OnInitializeFailed?.Invoke(error);

        public static void DispatchTransactionStart(string customAttribute) => OnTransactionStart?.Invoke(customAttribute);
        public static void DispatchTransactionEnd(string customAttribute) => OnTransactionEnd?.Invoke(customAttribute);
        public static void DispatchTransactionDeferred(string customAttribute) => OnTransactionDeferred?.Invoke(customAttribute);

        public static void DispatchPurchaseSuccess(string productId, string transactionId, PendingOrder pendingOrder, string customAttribute) => OnPurchaseSuccess?.Invoke(productId, transactionId, pendingOrder, customAttribute);
        public static void DispatchPurchaseFailed(string productId, string reason, string customAttribute) => OnPurchaseFailed?.Invoke(productId, reason, customAttribute);
    }
}
#endif
