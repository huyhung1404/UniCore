#if ENABLE_UNI_PURCHASE
using System;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UniPurchase
{
    public static class PurchaseEventDispatcher
    {
        public static event Action OnInitializeSuccess;
        public static event Action<string> OnInitializeFailed;

        public static event Action OnTransactionStart;
        public static event Action OnTransactionEnd;
        public static event Action OnTransactionDeferred;

        public static event Action<string, string, PendingOrder> OnPurchaseSuccess;
        public static event Action<string, string> OnPurchaseFailed;

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

        public static void DispatchTransactionStart() => OnTransactionStart?.Invoke();
        public static void DispatchTransactionEnd() => OnTransactionEnd?.Invoke();
        public static void DispatchTransactionDeferred() => OnTransactionDeferred?.Invoke();

        public static void DispatchPurchaseSuccess(string productId, string transactionId, PendingOrder pendingOrder) => OnPurchaseSuccess?.Invoke(productId, transactionId, pendingOrder);
        public static void DispatchPurchaseFailed(string productId, string reason) => OnPurchaseFailed?.Invoke(productId, reason);
    }
}
#endif