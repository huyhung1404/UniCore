#if ENABLE_UNI_PURCHASE
using UnityEngine;

namespace UniPurchase
{
    public abstract class BaseRewardHandler : MonoBehaviour
    {
        [SerializeField] private PurchaseService m_purchaseService;

        protected virtual void OnEnable()
        {
            PurchaseEventDispatcher.OnPurchaseSuccess += HandlePurchaseSuccess;
        }

        protected virtual void OnDisable()
        {
            PurchaseEventDispatcher.OnPurchaseSuccess -= HandlePurchaseSuccess;
        }

        private void HandlePurchaseSuccess(string productId, string transactionId)
        {
            if (string.IsNullOrEmpty(productId)) return;

            if (IsTransactionProcessedInSave(transactionId))
            {
                Debug.LogWarning($"[UniPurchase] Transaction {transactionId} already processed.");
                m_purchaseService?.ConfirmTransaction(transactionId);
                return;
            }

            var isRewardSuccess = ProcessProjectSpecificRewards(productId);
            if (!isRewardSuccess) return; 

            var isSaveSuccess = SaveTransactionAndGameData(transactionId);
            if (!isSaveSuccess) return; 

            m_purchaseService?.ConfirmTransaction(transactionId);
        }

        protected abstract bool IsTransactionProcessedInSave(string transactionId);

        protected abstract bool SaveTransactionAndGameData(string transactionId);

        protected abstract bool ProcessProjectSpecificRewards(string productId);
    }
}
#endif