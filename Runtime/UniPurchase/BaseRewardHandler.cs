#if ENABLE_UNI_PURCHASE
using UnityEngine;

namespace UniPurchase
{
    public abstract class BaseRewardHandler
    {
        public virtual void Initialize()
        {
            PurchaseEventDispatcher.OnPurchaseSuccess += HandlePurchaseSuccess;
        }
        
        public virtual void Dispose()
        {
            PurchaseEventDispatcher.OnPurchaseSuccess -= HandlePurchaseSuccess;
        }

        private void HandlePurchaseSuccess(string productId, string transactionId)
        {
            if (string.IsNullOrEmpty(productId)) return;

            if (IsTransactionProcessedInSave(transactionId))
            {
                Debug.LogWarning($"[UniPurchase] Transaction {transactionId} already processed.");
                PurchaseService.ConfirmTransaction(transactionId);
                return;
            }

            var isRewardSuccess = ProcessProjectSpecificRewards(productId);
            if (!isRewardSuccess)
            {
                PurchaseService.RejectTransaction(transactionId, "Reward processing failed");
                return;
            }

            var isSaveSuccess = SaveTransactionAndGameData(transactionId);
            if (!isSaveSuccess)
            {
                PurchaseService.RejectTransaction(transactionId, "Save failed");
                return;
            }

            PurchaseService.ConfirmTransaction(transactionId);
        }

        protected abstract bool IsTransactionProcessedInSave(string transactionId);
        protected abstract bool SaveTransactionAndGameData(string transactionId);
        protected abstract bool ProcessProjectSpecificRewards(string productId);
    }
}
#endif