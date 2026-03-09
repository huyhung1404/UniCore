#if ENABLE_UNI_PURCHASE
using UnityEngine;

namespace UniPurchase
{
    public static class LocalTransactionTracker
    {
        private const string k_transactionKeyPrefix = "IAP_TXN_";

        public static bool IsTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;

            var key = k_transactionKeyPrefix + transactionId;
            var isProcessed = PlayerPrefs.GetInt(key, 0) == 1;

            return isProcessed;
        }

        public static void MarkTransactionAsProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return;

            var key = k_transactionKeyPrefix + transactionId;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
    }
}
#endif