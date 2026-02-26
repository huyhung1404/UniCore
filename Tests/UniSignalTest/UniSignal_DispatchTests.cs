using NUnit.Framework;
using UniCore.Signal;
using UnityEngine;

namespace UniCore.Tests.Signal
{
    public class UniSignal_DispatchTests
    {
        private struct TestSignal : ISignalEvent
        {
        }

        private class AllListener : ISignalListener<TestSignal>
        {
            public bool Received;

            public void OnSignal(TestSignal signal)
            {
                Received = true;
            }
        }

        private class GamePlayListener : ISignalListener<TestSignal>
        {
            public bool Received;
            public SignalScope ListenScope => UniSignal_ScopeTests.s_Gameplay;

            public void OnSignal(TestSignal signal)
            {
                Received = true;
            }
        }

        private class UIListener : ISignalListener<TestSignal>
        {
            public bool Received;
            public SignalScope ListenScope => UniSignal_ScopeTests.s_UI;

            public void OnSignal(TestSignal signal)
            {
                Received = true;
            }
        }

        [Test]
        public void UniSignal_Dispatch_ShouldNotifyListener_Test()
        {
            var allListener = new AllListener();
            var gamePlayListener = new GamePlayListener();
            var uiListener = new UIListener();

            SignalSystem.Register(allListener);
            SignalSystem.Register(gamePlayListener);
            SignalSystem.Register(uiListener);

            SignalSystem.Dispatch(new TestSignal());

            Assert.IsTrue(allListener.Received);
            Assert.IsTrue(gamePlayListener.Received);
            Assert.IsTrue(uiListener.Received);

            SignalSystem.Unregister(allListener);
            SignalSystem.Unregister(gamePlayListener);
            SignalSystem.Unregister(uiListener);
        }

        [Test]
        public void UniSignal_Dispatch_ShouldNotNotify_WhenScopeDoesNotMatch_Test()
        {
            var allListener = new AllListener();
            var gamePlayListener = new GamePlayListener();
            var uiListener = new UIListener();

            SignalSystem.Register(allListener);
            SignalSystem.Register(gamePlayListener);
            SignalSystem.Register(uiListener);

            SignalSystem.Dispatch(new TestSignal(), UniSignal_ScopeTests.s_Gameplay);

            Assert.IsTrue(allListener.Received);
            Assert.IsTrue(gamePlayListener.Received);
            Assert.IsFalse(uiListener.Received);

            SignalSystem.Unregister(allListener);
            SignalSystem.Unregister(gamePlayListener);
            SignalSystem.Unregister(uiListener);
        }

        [Test]
        public void UniSignal_Dispatch_ShouldRespectPriorityOrder_Test()
        {
            var order = new System.Collections.Generic.List<int>();

            var low = new OrderedListener(0, order);
            var high = new OrderedListener(100, order);

            SignalSystem.Register(low);
            SignalSystem.Register(high);

            SignalSystem.Dispatch(new TestSignal());

            Assert.AreEqual(100, order[0]);
            Assert.AreEqual(0, order[1]);

            SignalSystem.Unregister(low);
            SignalSystem.Unregister(high);
        }

        private class OrderedListener : ISignalListener<TestSignal>
        {
            private readonly System.Collections.Generic.List<int> _order;

            public OrderedListener(int priority, System.Collections.Generic.List<int> order)
            {
                Priority = priority;
                _order = order;
            }

            public int Priority { get; }

            public void OnSignal(TestSignal signal)
            {
                _order.Add(Priority);
            }
        }

        [Test]
        public void UniSignal_Dispatch_ShouldWorkWithMultipleScopes_Test()
        {
            var listener = new TestListener();
            SignalSystem.Register(listener);

            SignalSystem.Dispatch(new TestSignalMultiScope());

            Assert.IsTrue(listener.Received);

            SignalSystem.Unregister(listener);
        }

        private class TestListener : ISignalListener<TestSignalMultiScope>
        {
            public bool Received;
            public int Priority => 0;
            public SignalScope ListenScope => UniSignal_ScopeTests.s_Gameplay;

            public void OnSignal(TestSignalMultiScope signal)
            {
                Received = true;
            }
        }

        private struct TestSignalMultiScope : ISignalEvent
        {
            public SignalScope Scope => UniSignal_ScopeTests.s_Gameplay | UniSignal_ScopeTests.s_UI;
        }

        [Test]
        public void UniSignal_Dispatch_ShouldNotifyAllListeners_Test()
        {
            var a = new TestListener();
            var b = new TestListener();

            SignalSystem.Register(a);
            SignalSystem.Register(b);

            SignalSystem.Dispatch(new TestSignalMultiScope());

            Assert.IsTrue(a.Received);
            Assert.IsTrue(b.Received);

            SignalSystem.Unregister(a);
            SignalSystem.Unregister(b);
        }

        [Test]
        public void UniSignal_Dispatch_ShouldContinue_WhenListenerThrows_Test()
        {
            var good = new TestListener();
            var bad = new ExceptionListener();

            SignalSystem.Register(bad);
            SignalSystem.Register(good);
            Assert.DoesNotThrow(() =>
            {
                Debug.unityLogger.logEnabled = false;
                SignalSystem.Dispatch(new TestSignalMultiScope());
                Debug.unityLogger.logEnabled = true;
            });
            Assert.IsTrue(good.Received);

            SignalSystem.Unregister(bad);
            SignalSystem.Unregister(good);
        }

        private class ExceptionListener : ISignalListener<TestSignalMultiScope>
        {
            public int Priority => 0;
            public SignalScope ListenScope => UniSignal_ScopeTests.s_Gameplay;

            public void OnSignal(TestSignalMultiScope signal)
            {
                throw new System.Exception("Boom");
            }
        }
    }
}