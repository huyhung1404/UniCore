#if ENABLE_UNI_PURCHASE
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace UniPurchase.Tests
{
    public class UniPurchase_Component_Tests
    {
        [SetUp]
        public void SetUp()
        {
            // Reset dữ liệu ổ cứng ảo trước mỗi test
            PlayerPrefs.DeleteAll();
        }

        // ==========================================
        // 1. TEST CASES: LOCAL TRANSACTION TRACKER
        // ==========================================

        [Test]
        public void LocalTransactionTracker_When_Marked_Should_Return_True()
        {
            var testId = "txn_12345";
            
            // ACT & ASSERT: Trước khi đánh dấu phải là false
            var isProcessedBefore = LocalTransactionTracker.IsTransactionProcessed(testId);
            Assert.IsFalse(isProcessedBefore, "Transaction must not be processed initially.");

            // ACT: Đánh dấu giao dịch
            LocalTransactionTracker.MarkTransactionAsProcessed(testId);

            // ASSERT: Sau khi đánh dấu phải là true
            var isProcessedAfter = LocalTransactionTracker.IsTransactionProcessed(testId);
            Assert.IsTrue(isProcessedAfter, "Transaction must be true after marking.");
        }

        [Test]
        public void LocalTransactionTracker_With_Empty_Id_Should_Safely_Return_False()
        {
            // ACT
            var result = LocalTransactionTracker.IsTransactionProcessed("");
            
            // ASSERT: Các ID rỗng hoặc null phải bị cản lại (Early return)
            Assert.IsFalse(result, "Empty transaction ID should safely return false without exceptions.");
        }

        // ==========================================
        // 2. TEST CASES: PURCHASE VALIDATOR
        // ==========================================

        [Test]
        public void PurchaseValidator_With_Empty_Receipt_Should_Always_Return_False()
        {
            // ARRANGE: Tạo validator rỗng (Bypass key)
            var validator = new PurchaseValidator(null, null);
            
            // ACT
            var result = validator.IsReceiptValid("");
            
            // ASSERT: Hacker gửi biên lai rỗng phải bị chặn đứng
            Assert.IsFalse(result, "Empty receipt must return false to prevent bypass attacks.");
        }

        // ==========================================
        // 3. TEST CASES: PURCHASE EVENT DISPATCHER
        // ==========================================

        [Test]
        public void PurchaseEventDispatcher_ResetEvents_Should_Clear_All_Subscribers()
        {
            // ARRANGE: Đăng ký một event lắng nghe
            var isEventTriggered = false;
            System.Action onStartAction = () => isEventTriggered = true;
            
            PurchaseEventDispatcher.OnTransactionStart += onStartAction;
            
            // ACT: Dùng Reflection gọi hàm dọn rác tĩnh (thường chạy lúc Unity Reload Domain)
            var resetMethod = typeof(PurchaseEventDispatcher).GetMethod("ResetEvents", BindingFlags.NonPublic | BindingFlags.Static);
            resetMethod?.Invoke(null, null);

            // Bắn event sau khi đã Reset
            PurchaseEventDispatcher.DispatchTransactionStart();

            // ASSERT: Action không được phép chạy vì đã bị xoá khỏi bộ nhớ
            Assert.IsFalse(isEventTriggered, "Subscribers must be cleared after ResetEvents is called to prevent memory leaks.");
        }

        // ==========================================
        // 4. TEST CASES: SERVICE EDGE CASES (SUBSCRIPTION)
        // ==========================================

        [Test]
        public async Task PurchaseService_CheckSubscription_When_Not_Initialized_Should_Return_False()
        {
            // ARRANGE: Đảm bảo Service hoàn toàn trắng tinh (chưa Init)
            var instanceField = typeof(PurchaseService).GetField("s_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);

            // ACT: Cố tình hỏi xem user có đang là VIP không khi game vừa mở (chưa kết nối Apple/Google)
            var isActive = await PurchaseService.CheckIsSubscriptionActiveAsync("com.vip.month");

            // ASSERT: Phải trả về false an toàn thay vì văng NullReferenceException
            Assert.IsFalse(isActive, "Should safely return false if system is offline or not initialized.");
        }
    }
}
#endif