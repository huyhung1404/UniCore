using NUnit.Framework;
using UniCore.Signal;

namespace UniCore.Tests.Signal
{
    public class UniSignal_ScopeTests
    {
        public static readonly SignalScope s_Gameplay = new(1UL << 0);
        public static readonly SignalScope s_UI = new(1UL << 1);

        [Test]
        public void UniSignal_MatchScope_Test()
        {
            var listenerAllScope = SignalScope.All;
            var listenerGameplayScope = s_Gameplay;
            var listenerUIScope = s_UI;

            var sendAll = SignalScope.All;
            Assert.IsTrue(listenerAllScope.Intersects(sendAll));
            Assert.IsTrue(listenerGameplayScope.Intersects(sendAll));
            Assert.IsTrue(listenerUIScope.Intersects(sendAll));

            var sendGameplay = s_Gameplay;
            Assert.IsTrue(listenerAllScope.Intersects(sendGameplay));
            Assert.IsTrue(listenerGameplayScope.Intersects(sendGameplay));
            Assert.IsFalse(listenerUIScope.Intersects(sendGameplay));

            var sendUI = s_UI;
            Assert.IsTrue(listenerAllScope.Intersects(sendUI));
            Assert.IsFalse(listenerGameplayScope.Intersects(sendUI));
            Assert.IsTrue(listenerUIScope.Intersects(sendUI));
        }
    }
}