using UnityEngine;

namespace UniConsole
{
    public static class DeveloperAuthenticator
    {
        internal static ConsoleSettings RuntimeSettings { get; private set; }
        internal static bool IsDeveloperMode { get; set; }
        private static GameObject go;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            RuntimeSettings = ConsoleRuntimeSettings.GetInstance(ConsoleRuntimeSettings.k_FileName).CurrentData;
            
            if (!RuntimeSettings.m_isSystemEnabled) return;

            IsDeveloperMode = PlayerPrefs.GetInt("unicore_flag", 0) == 1;
            go = new GameObject("Developer Authenticator");
            Object.DontDestroyOnLoad(go);

            if (IsDeveloperMode)
            {
                GenerateConsole();
                return;
            }

            if (RuntimeSettings.m_loginTriggerMode == TriggerMode.None) return;
            GenerateLogin();
        }

        private static ConsoleManager GenerateConsole()
        {
            var loginGUI = go.GetComponent<LoginGUI>();
            if (loginGUI != null) Object.Destroy(loginGUI);
            
            var consoleManager = go.GetComponent<ConsoleManager>();
            if (consoleManager == null)
            {
                consoleManager = go.AddComponent<ConsoleManager>();
                consoleManager.Initialize(RuntimeSettings);
            }
            return consoleManager;
        }

        private static LoginGUI GenerateLogin()
        {
            var loginGUI = go.GetComponent<LoginGUI>();
            if (loginGUI != null) return loginGUI;
            
            loginGUI = go.AddComponent<LoginGUI>();
            loginGUI.Initialize(RuntimeSettings);
            return loginGUI;
        }

        public static void OpenConsole()
        {
            if (!IsDeveloperMode) return;

            var consoleManager = go.GetComponent<ConsoleManager>();
            if (consoleManager == null)
            {
                consoleManager = GenerateConsole();
            }

            consoleManager.Open();
        }

        public static void OpenLogin()
        {
            GenerateLogin().Open();
        }
    }
}