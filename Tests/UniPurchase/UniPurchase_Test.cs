#if ENABLE_UNI_PURCHASE
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UniPurchase.Tests
{
    public class UniPurchase_Test
    {
        private const string k_testProductId = "com.test.gem100";
        private const string k_testTransactionId = "txn_mock_8888";

        private MockRewardHandler _mockRewardHandler;
        private PurchaseConfig _testConfig;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteAll();

            var resetMethod = typeof(PurchaseEventDispatcher).GetMethod("ResetEvents", BindingFlags.NonPublic | BindingFlags.Static);
            resetMethod?.Invoke(null, null);

            var instanceField = typeof(PurchaseService).GetField("s_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            _testConfig = ScriptableObject.CreateInstance<PurchaseConfig>();
            var mockProduct = ScriptableObject.CreateInstance<ProductData>();
            mockProduct.WithProductId(k_testProductId)
                .WithProductType(PurchaseProductType.Consumable);

            _testConfig.SetUp(true, new System.Collections.Generic.List<ProductData> { mockProduct });
            _mockRewardHandler = new MockRewardHandler();
            _mockRewardHandler.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _mockRewardHandler.Dispose();
            Object.DestroyImmediate(_testConfig);
        }

        private object GetPurchaseServiceInstance()
        {
            var prop = typeof(PurchaseService).GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static);
            return prop?.GetValue(null);
        }

        // ==========================================
        // TEST CASES: GỌI TRỰC TIẾP VÀO PURCHASE SERVICE
        // ==========================================

        [UnityTest]
        public IEnumerator BuyProduct_With_Empty_Id_Should_Return_Early()
        {
            PurchaseService.BuyProduct("");

            yield return null;

            Assert.IsFalse(PurchaseService.IsProcessing, "Transaction should not start for empty ID.");
        }

        [UnityTest]
        public IEnumerator BuyProduct_When_Not_Initialized_Should_Trigger_AutoRecovery_And_Fail()
        {
            var isFailedEventFired = false;
            PurchaseEventDispatcher.OnPurchaseFailed += (id, reason, _) => isFailedEventFired = true;

            PurchaseService.BuyProduct(k_testProductId);

            yield return new WaitForSeconds(0.1f);

            Assert.IsTrue(isFailedEventFired, "Auto-recovery should fail gracefully and fire OnPurchaseFailed.");
            Assert.IsFalse(PurchaseService.IsProcessing, "Should not be processing after failed recovery.");
        }

        [UnityTest]
        public IEnumerator BuyProduct_When_Already_Processing_Should_Be_Blocked()
        {
            var serviceInstance = GetPurchaseServiceInstance();
            var activeTxField = typeof(PurchaseService).GetField("_activeTransactions", BindingFlags.NonPublic | BindingFlags.Instance);
            activeTxField?.SetValue(serviceInstance, 1);

            LogAssert.Expect(LogType.Warning, "[UniPurchase] A transaction is already in progress.");

            PurchaseService.BuyProduct(k_testProductId);

            yield return null;

            Assert.AreEqual(1, activeTxField?.GetValue(serviceInstance), "Active transactions count must remain 1.");
        }

        [UnityTest]
        public IEnumerator RestorePurchases_When_Processing_Should_Return_Early()
        {
            var serviceInstance = GetPurchaseServiceInstance();
            var activeTxField = typeof(PurchaseService).GetField("_activeTransactions", BindingFlags.NonPublic | BindingFlags.Instance);
            activeTxField?.SetValue(serviceInstance, 1);

            PurchaseService.RestorePurchases();

            yield return null;

            Assert.AreEqual(1, activeTxField?.GetValue(serviceInstance), "Restore should early return if already processing.");
        }

        // ==========================================
        // TEST CASES: LUỒNG SỰ KIỆN (EVENT FLOW)
        // ==========================================

        [UnityTest]
        public IEnumerator PurchaseFlow_Success_Should_Give_Reward_And_Save()
        {
            PurchaseEventDispatcher.DispatchPurchaseSuccess(k_testProductId, k_testTransactionId, null);
            yield return null;

            Assert.IsTrue(_mockRewardHandler.IsRewardGiven, "Reward should be given.");
            Assert.IsTrue(_mockRewardHandler.IsGameSaved, "Game data must be saved.");
            Assert.IsTrue(LocalTransactionTracker.IsTransactionProcessed(k_testTransactionId), "Saved to PlayerPrefs.");
        }

        [UnityTest]
        public IEnumerator PurchaseFlow_Duplicate_Should_Be_Blocked_By_Idempotency()
        {
            LocalTransactionTracker.MarkTransactionAsProcessed(k_testTransactionId);
            PurchaseEventDispatcher.DispatchPurchaseSuccess(k_testProductId, k_testTransactionId, null);
            yield return null;

            Assert.IsFalse(_mockRewardHandler.IsRewardGiven, "Duplicate reward blocked.");
            Assert.IsFalse(_mockRewardHandler.IsGameSaved, "Game save blocked.");
        }

        [UnityTest]
        public IEnumerator PurchaseFlow_Failed_Should_Not_Trigger_Rewards()
        {
            PurchaseEventDispatcher.DispatchPurchaseFailed(k_testProductId, "User cancelled", null);
            yield return null;

            Assert.IsFalse(_mockRewardHandler.IsRewardGiven, "Failed purchases should never give rewards.");
            Assert.IsFalse(LocalTransactionTracker.IsTransactionProcessed(k_testTransactionId), "Failed transactions not saved.");
        }

        // ==========================================
        // TEST CASES: ĐÂM XUYÊN SINGLETON (WHITE-BOX TESTING)
        // ==========================================

        [UnityTest]
        public IEnumerator Reflection_HandleChannelPurchasePending_Should_Cache_Transaction()
        {
            var serviceInstance = GetPurchaseServiceInstance();

            typeof(PurchaseService).GetField("_isInitialized", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(serviceInstance, true);

            var isSuccessEventFired = false;
            PurchaseEventDispatcher.OnPurchaseSuccess += (pid, tid, _) => isSuccessEventFired = true;

            var handleMethod = typeof(PurchaseService).GetMethod("HandleChannelPurchasePending", BindingFlags.NonPublic | BindingFlags.Instance);
            handleMethod?.Invoke(serviceInstance, new object[] { k_testProductId, k_testTransactionId });

            yield return null;

            Assert.IsTrue(isSuccessEventFired, "HandleChannelPurchasePending should dispatch Success Event.");

            var unconfirmedField = typeof(PurchaseService).GetField("_unconfirmedTransactions", BindingFlags.NonPublic | BindingFlags.Instance);
            var unconfirmedTransactions = unconfirmedField?.GetValue(serviceInstance) as System.Collections.Generic.Dictionary<string, string>;

            Assert.IsNotNull(unconfirmedTransactions, "Unconfirmed transactions dictionary should not be null.");
            Assert.IsTrue(unconfirmedTransactions.ContainsKey(k_testTransactionId), "The transaction MUST be cached in _unconfirmedTransactions.");
        }

        [UnityTest]
        public IEnumerator Reflection_ConfirmTransaction_Should_Clear_Transaction_From_Cache()
        {
            var serviceInstance = GetPurchaseServiceInstance();

            var unconfirmedField = typeof(PurchaseService).GetField("_unconfirmedTransactions", BindingFlags.NonPublic | BindingFlags.Instance);
            var unconfirmedTransactions = unconfirmedField?.GetValue(serviceInstance) as System.Collections.Generic.Dictionary<string, string>;

            if (unconfirmedTransactions != null) unconfirmedTransactions[k_testTransactionId] = k_testProductId;

            Assert.IsTrue(unconfirmedTransactions.ContainsKey(k_testTransactionId), "Setup failed: Transaction not injected.");

            try
            {
                PurchaseService.ConfirmTransaction(k_testTransactionId);
            }
            catch { /* Bỏ qua lỗi Native C++ nếu có */ }

            yield return null;

            Assert.IsFalse(unconfirmedTransactions.ContainsKey(k_testTransactionId), "ConfirmTransaction MUST remove the transaction from _unconfirmedTransactions cache.");
        }

        // ==========================================
        // MOCK CLASSES CHO TESTING
        // ==========================================

        private class MockRewardHandler : BaseRewardHandler
        {
            public bool IsRewardGiven { get; private set; }
            public bool IsGameSaved { get; private set; }

            protected override bool IsTransactionProcessedInSave(string transactionId)
            {
                return LocalTransactionTracker.IsTransactionProcessed(transactionId);
            }

            protected override bool ProcessProjectSpecificRewards(string productId)
            {
                if (productId == k_testProductId)
                {
                    IsRewardGiven = true;
                    return true;
                }
                return false;
            }

            protected override bool SaveTransactionAndGameData(string transactionId)
            {
                IsGameSaved = true;
                LocalTransactionTracker.MarkTransactionAsProcessed(transactionId);
                return true;
            }
        }
    }
}
#endif
