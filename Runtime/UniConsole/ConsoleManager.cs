using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace UniConsole
{
    public class ConsoleManager : MonoBehaviour
    {
        private static ConsoleManager s_instance;

        private ConsoleSettings _settings;
        private ConsoleMemory _memory;
        private ConsoleCommandProcessor _commandProcessor;
        private ConsoleGUI _gui;
        private ControlTrigger _controlTrigger;

        private bool _isConsoleInitialized;

        private float _fpsTimer;
        private int _fpsFrames;
        private float _currentFps;
        private float _currentRamMB;

        public void Initialize(ConsoleSettings settings)
        {
            _settings = settings;
            s_instance = this;
            _controlTrigger = new ControlTrigger(settings.m_openTriggerMode, _settings.m_openTapCount, _settings.m_openTapTimeout, _settings.m_openLongPressDuration);

            _gui = new ConsoleGUI
            {
                OnCloseRequested = () => _controlTrigger.IsOpen = false,
                OnSubmitCommand = ProcessCommand,
                OnCancelCommand = CancelCommand
            };

            InitializeDeveloperModeMemory();
        }

        private void Start()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(this);
            }
        }

        public void Open()
        {
            _controlTrigger.IsOpen = true;
        }

        private void OnDestroy()
        {
            _gui?.DestroyTextures();
            if (!_isConsoleInitialized) return;
            Application.logMessageReceived -= HandleLogMessage;
        }

        private void InitializeDeveloperModeMemory()
        {
            if (_isConsoleInitialized) return;

            _memory = new ConsoleMemory(_settings.m_maxLogs);
            _commandProcessor = new ConsoleCommandProcessor();
            _commandProcessor.Initialize();

            Application.logMessageReceived += HandleLogMessage;
            _isConsoleInitialized = true;
        }

        private void Update()
        {
            if (_settings.m_enableMiniProfiler)
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

            if (_settings.m_openConsoleKey != KeyCode.None)
            {
                if (Input.GetKeyDown(_settings.m_openConsoleKey))
                {
                    _controlTrigger.IsOpen = !_controlTrigger.IsOpen;
                    return;
                }
            }

            var triggerResult = _controlTrigger.CheckTriggers();

            if (triggerResult == TriggerResult.Request)
            {
                _controlTrigger.IsOpen = true;
            }
        }

        private void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            var shouldCaptureTrace = false;

            if (type == LogType.Log) shouldCaptureTrace = _settings.m_captureLogStackTrace;
            else if (type == LogType.Warning) shouldCaptureTrace = _settings.m_captureWarningStackTrace;
            else if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) shouldCaptureTrace = _settings.m_captureErrorStackTrace;

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
            var baseScale = Mathf.Min(Screen.width, Screen.height) / _settings.m_referenceMinDimension;
            var finalScale = baseScale * _settings.m_guiScaleMultiplier;

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1f));
            var virtualWidth = Screen.width / finalScale;
            var virtualHeight = Screen.height / finalScale;

            _gui.InitializeStyles(_settings.m_guiOpacity, _settings.m_profilerOpacity);

            if (_settings.m_enableMiniProfiler)
            {
                _gui.DrawMiniProfiler(_currentFps, _currentRamMB, virtualWidth, virtualHeight);
            }

            if (!_controlTrigger.IsOpen || !_isConsoleInitialized) return;

            _gui.DrawConsole(_memory, _commandProcessor, virtualWidth, virtualHeight);
        }
    }
}