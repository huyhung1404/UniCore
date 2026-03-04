using System;
using UnityEngine;
using UnityEngine.Profiling; // Thêm thư viện Profiler

namespace UniCore.Console
{
    public class ConsoleManager : MonoBehaviour
    {
        private static ConsoleManager s_instance;

        private ConsoleRuntimeSettings _runtimeSettingsAsset;
        private DeveloperAuthenticator _authenticator;
        private ConsoleMemory _memory;
        private ConsoleCommandProcessor _commandProcessor;
        private ConsoleGUI _gui;
        
        private bool _isConsoleOpen;
        private bool _isConsoleInitialized; 

        private float _fpsTimer;
        private int _fpsFrames;
        private float _currentFps;
        private float _currentRamMB;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var go = new GameObject("[ConsoleManager]");
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<ConsoleManager>();
        }

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return; 
            }

            _authenticator = new DeveloperAuthenticator(GetLiveSettings);
            _gui = new ConsoleGUI();
            _gui.OnCloseRequested = () => _isConsoleOpen = false;
            _gui.OnSubmitCommand = ProcessCommand;
            _gui.OnCancelCommand = CancelCommand;
            
            if (_authenticator.IsDeveloperMode)
            {
                InitializeDeveloperModeMemory();
            }
        }

        private void OnDestroy()
        {
            _gui?.DestroyTextures();
            if (!_isConsoleInitialized) return; 
            Application.logMessageReceived -= HandleLogMessage;
        }

        private ConsoleSettings GetLiveSettings()
        {
            if (_runtimeSettingsAsset == null)
            {
                _runtimeSettingsAsset = ConsoleRuntimeSettings.GetInstance(ConsoleRuntimeSettings.k_FileName);
            }
            return _runtimeSettingsAsset.CurrentData;
        }

        public static void OpenConsole()
        {
            if (s_instance == null) return;
            
            if (!s_instance._authenticator.IsDeveloperMode)
            {
                s_instance._authenticator.CheckTriggers(); 
            }
            else
            {
                if (!s_instance._isConsoleInitialized) s_instance.InitializeDeveloperModeMemory();
                s_instance._isConsoleOpen = true;
            }
        }

        private void InitializeDeveloperModeMemory()
        {
            if (_isConsoleInitialized) return; 

            var settings = GetLiveSettings();
            _memory = new ConsoleMemory(settings.m_maxLogs);
            _commandProcessor = new ConsoleCommandProcessor();
            _commandProcessor.Initialize();

            Application.logMessageReceived += HandleLogMessage;
            _isConsoleInitialized = true;
        }

        private void Update()
        {
            var settings = GetLiveSettings();

            if (_authenticator.IsDeveloperMode && settings.m_enableMiniProfiler)
            {
                _fpsTimer += Time.unscaledDeltaTime;
                _fpsFrames++;
                
                if (_fpsTimer >= 0.5f) 
                {
                    _currentFps = _fpsFrames / _fpsTimer;
                    _currentRamMB = Profiler.GetTotalAllocatedMemoryLong() / 1048576f; 
                    _fpsTimer = 0f;
                    _fpsFrames = 0;
                }
            }

            if (_authenticator.IsDeveloperMode && settings.m_openConsoleKey != KeyCode.None)
            {
                if (Input.GetKeyDown(settings.m_openConsoleKey))
                {
                    _isConsoleOpen = !_isConsoleOpen;
                    if (_isConsoleOpen && !_isConsoleInitialized) InitializeDeveloperModeMemory();
                    return; 
                }
            }

            if (_authenticator.IsDeveloperMode && !_isConsoleInitialized)
            {
                InitializeDeveloperModeMemory(); 
                _isConsoleOpen = true; 
                return;
            }

            if (_isConsoleOpen) return;

            var triggerResult = _authenticator.CheckTriggers();

            if (triggerResult == TriggerResult.RequestOpenConsole)
            {
                _isConsoleOpen = true; 
            }
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            var settings = GetLiveSettings();
            var shouldCaptureTrace = false;
            
            if (type == LogType.Log) shouldCaptureTrace = settings.m_captureLogStackTrace;
            else if (type == LogType.Warning) shouldCaptureTrace = settings.m_captureWarningStackTrace;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) shouldCaptureTrace = settings.m_captureErrorStackTrace;

            _memory.AddLog(condition, stackTrace, type, false, shouldCaptureTrace, _gui.IsCollapsed);
            _gui.LogScrollPos.y = float.MaxValue;
        }

        private void ProcessCommand(string inputRaw)
        {
            _memory.AddLog($"> {inputRaw}", "", LogType.Log, true, false, false);

            var resultMsg = _commandProcessor.ProcessInput(inputRaw, out var logType, (cmdInfo, args) => 
            {
                try
                {
                    var invokeArgs = new object[cmdInfo.Parameters.Length];
                    for (var i = 0; i < cmdInfo.Parameters.Length; i++)
                    {
                        invokeArgs[i] = Convert.ChangeType(args[i], cmdInfo.Parameters[i].ParameterType);
                    }
                    cmdInfo.Method.Invoke(null, invokeArgs);
                }
                catch (Exception e)
                {
                    _memory.AddLog($"Execution failed: {e.Message}", "", LogType.Exception, true, true, false);
                }
            });

            if (!string.IsNullOrEmpty(resultMsg))
            {
                _memory.AddLog(resultMsg, "", logType, true, false, false);
            }
            
            _gui.LogScrollPos.y = float.MaxValue;
        }

        private void CancelCommand()
        {
            var msg = _commandProcessor.CancelPendingCommand();
            if (!string.IsNullOrEmpty(msg))
            {
                _memory.AddLog(msg, "", LogType.Warning, true, false, false);
            }
        }

        private void OnGUI()
        {
            var settings = GetLiveSettings();
            var baseScale = Mathf.Min(Screen.width, Screen.height) / settings.m_referenceMinDimension;
            var finalScale = baseScale * settings.m_guiScaleMultiplier;
            
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1f));
            var virtualWidth = Screen.width / finalScale;
            var virtualHeight = Screen.height / finalScale;

            _gui.InitializeStyles(settings.m_guiOpacity, settings.m_profilerOpacity);

            if (_authenticator.IsDeveloperMode && settings.m_enableMiniProfiler)
            {
                _gui.DrawMiniProfiler(_currentFps, _currentRamMB, virtualWidth, virtualHeight);
            }

            if (_authenticator.IsLoginOpen)
            {
                _authenticator.DrawLoginPanel(virtualWidth, virtualHeight, new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
                return; 
            }

            if (!_isConsoleOpen || !_isConsoleInitialized) return; 

            _gui.DrawConsole(_memory, _commandProcessor, virtualWidth, virtualHeight);
        }
    }
}