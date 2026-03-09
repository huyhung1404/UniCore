#if ENABLE_UNI_PURCHASE
using UnityEngine;
using UnityEngine.Purchasing.Security;

namespace UniPurchase
{
    public sealed class PurchaseValidator
    {
        private CrossPlatformValidator _validator;
        private bool _isValidatingEnabled;

        public PurchaseValidator(byte[] appleTangleData, byte[] googleTangleData)
        {
            var isEditor = Application.isEditor;
            if (isEditor) return; 

#if !UNITY_EDITOR
            if (appleTangleData != null && googleTangleData != null)
            {
                var appIdentifier = Application.identifier;
                _validator = new CrossPlatformValidator(googleTangleData, appleTangleData, appIdentifier);
                _isValidatingEnabled = true;
            }
            else
            {
                Debug.LogWarning("[UniPurchase] Missing Tangle Data. Validation bypassed.");
                _isValidatingEnabled = false;
            }
#endif
        }

        public bool IsReceiptValid(string receipt)
        {
            if (string.IsNullOrEmpty(receipt)) return false;
            
            var isEditor = Application.isEditor;
            if (isEditor || !_isValidatingEnabled) return true; 

            try
            {
                var result = _validator.Validate(receipt);
                var isValid = result != null && result.Length > 0;
                return isValid;
            }
            catch (IAPSecurityException)
            {
                return false;
            }
        }
    }
}
#endif