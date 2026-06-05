#if ENABLE_UNI_PURCHASE
using System;
using UnityEngine;
using UnityEngine.Purchasing.Security;

namespace UniPurchase
{
    public sealed class PurchaseValidator
    {
        private CrossPlatformValidator _validator;
        private bool _isValidatingEnabled;

        public PurchaseValidator(Func<byte[]> googleTangleData)
        {
            var isEditor = Application.isEditor;
            if (isEditor) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            var tangleData = googleTangleData?.Invoke();
            if (tangleData != null)
            {
                var appIdentifier = Application.identifier;
                // Only validate for Google Play locally.
                _validator = new CrossPlatformValidator(tangleData, null, appIdentifier);
                _isValidatingEnabled = true;
            }
            else
            {
                Debug.LogWarning("[UniPurchase] Missing Google Tangle Data. Validation bypassed.");
                _isValidatingEnabled = false;
            }
#endif
        }

        public bool IsReceiptValid(string receipt)
        {
            if (string.IsNullOrEmpty(receipt)) return false;
            
            var isEditor = Application.isEditor;
            if (isEditor) return true; 

#if UNITY_IOS
            // StoreKit 2 handles local validation automatically.
            // Client-side validation is bypassed here. Defer strict security to Server-Side Validation.
            return true;
#elif UNITY_ANDROID
            if (!_isValidatingEnabled) return true;

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
#else
            return true;
#endif
        }
    }
}
#endif