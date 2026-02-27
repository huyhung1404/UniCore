using UnityEditor;
using UniCore.Signal;

namespace UniCore.Editor.Testing
{
    public struct GameStartedSignal : ISignalEvent
    {
    }

    public struct PlayerTakeDamageSignal : ISignalEvent
    {
        public int Damage;
        public int Damage2;
        public int Damage3;
    }

    public struct BossDefeatedSignal : ISignalEvent
    {
        public string BossId;
        public BossParam Param;
        public struct BossParam
        {
            public int param1;
            public int param2;
            public int param3;
        }
    }

    public class MockUIController : ISignalListener<GameStartedSignal>, ISignalListener<BossDefeatedSignal>
    {
        public int Priority { get; } = 15;

        public void OnSignal(GameStartedSignal signal)
        {
        }

        public void OnSignal(BossDefeatedSignal signal)
        {
        }
    }

    public class MockAudioController : ISignalListener<PlayerTakeDamageSignal>
    {
        public int Priority { get; } = 85;
        public SignalScope ListenScope { get; } = SignalVarsMockup.s_Gameplay;

        public void OnSignal(PlayerTakeDamageSignal signal)
        {
        }
    }

    public class PlayerVar
    {
        public int Health;
        public string Name;
        public bool IsGameOver;
        public PlayerParam Param;
        public struct PlayerParam
        {
            public int param1;
            public int param2;
            public int param3;
        }
    }

    public static class SignalVarsMockup
    {
        private const string k_autoInjectPrefsKey = "UniSignal.AutoInjectFakeData";
        public static readonly SignalScope s_Gameplay = new(1UL << 0);

        [MenuItem("UniCore/Debug/Auto Inject Fake Data", priority = 100)]
        public static void ToggleAutoInject()
        {
            var currentState = EditorPrefs.GetBool(k_autoInjectPrefsKey, false);
            EditorPrefs.SetBool(k_autoInjectPrefsKey, !currentState);
        }

        [MenuItem("UniCore/Debug/Auto Inject Fake Data", true)]
        public static bool ToggleAutoInjectValidate()
        {
            var isEnabled = EditorPrefs.GetBool(k_autoInjectPrefsKey, false);
            Menu.SetChecked("UniCore/Debug/Auto Inject Fake Data", isEnabled);
            return true;
        }

        public static void InjectFakeData()
        {
            SignalScopeRegistry.Register("GamePlay",s_Gameplay);
            var uiController = new MockUIController();
            SignalSystem.Register<GameStartedSignal>(uiController);
            SignalSystem.Register<BossDefeatedSignal>(uiController);

            var audioController = new MockAudioController();
            SignalSystem.Register(audioController);

            Vars.VarsSystem.Global.Define("PlayerHealth", 100, true);
            Vars.VarsSystem.Global.Define("PlayerName", "Hero", true);
            Vars.VarsSystem.Global.Define("IsGameOver", false, true);
            Vars.VarsSystem.Global.Define("Player", new PlayerVar(), true);
        }
    }
}