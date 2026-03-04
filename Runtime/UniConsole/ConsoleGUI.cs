using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniCore.Console
{
    public class ConsoleGUI
    {
        private const string k_PrefIsCollapsed = "UniConsole_IsCollapsed";
        private const string k_PrefShowCommand = "UniConsole_ShowCommand";
        private const string k_PrefShowLog = "UniConsole_ShowLog";
        private const string k_PrefShowWarning = "UniConsole_ShowWarning";
        private const string k_PrefShowError = "UniConsole_ShowError";
        private const string k_PrefProfilerX = "UniConsole_ProfilerX";
        private const string k_PrefProfilerY = "UniConsole_ProfilerY";

        private GUIStyle _mainAreaStyle;
        private GUIStyle _toolbarStyle;
        private GUIStyle _toolbarButtonStyle;
        private GUIStyle _searchFieldStyle;
        private GUIStyle _logItemStyle;
        private GUIStyle _logItemSelectedStyle;
        private GUIStyle _detailBoxStyle;
        private GUIStyle _cancelButtonStyle;
        private GUIStyle _promptLabelStyle;
        private GUIStyle _historyButtonStyle;
        private GUIStyle _profilerStyle;

        private Texture2D _selectedBgTexture;
        private Texture2D _normalBgTexture;
        private Texture2D _panelBgTexture;
        private Texture2D _promptBgTexture;

        public string ConsoleInput = "";
        public string SearchString = "";
        public string CommandSearchString = "";

        public bool IsCollapsed;
        public bool ShowCommand = true;
        public bool ShowLog = true;
        public bool ShowWarning = true;
        public bool ShowError = true;
        public bool ShowSuggestions;

        public Vector2 LogScrollPos;
        public Vector2 DetailScrollPos;
        public Vector2 SuggestionScrollPos;
        public ConsoleLogMessage SelectedLog;

        private readonly string[] _commandHistory = new string[15];
        private int _historyCount;
        private int _historyHead;
        private int _currentHistoryViewIndex = -1;

        private float _profilerPosX = -1f;
        private float _profilerPosY = 5f;
        private bool _isDraggingProfiler;
        private Vector2 _profilerDragOffset;
        private float _lastConsoleOpacity = -1f;
        private float _lastProfilerOpacity = -1f;

        public Action OnCloseRequested;
        public Action<string> OnSubmitCommand;
        public Action OnCancelCommand;

        public ConsoleGUI()
        {
            LoadPreferences();
        }

        private void LoadPreferences()
        {
            IsCollapsed = PlayerPrefs.GetInt(k_PrefIsCollapsed, 0) == 1;
            ShowCommand = PlayerPrefs.GetInt(k_PrefShowCommand, 1) == 1;
            ShowLog = PlayerPrefs.GetInt(k_PrefShowLog, 1) == 1;
            ShowWarning = PlayerPrefs.GetInt(k_PrefShowWarning, 1) == 1;
            ShowError = PlayerPrefs.GetInt(k_PrefShowError, 1) == 1;
            _profilerPosX = PlayerPrefs.GetFloat(k_PrefProfilerX, -1f);
            _profilerPosY = PlayerPrefs.GetFloat(k_PrefProfilerY, 5f);
        }

        private static void SavePreference(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void InitializeStyles(float consoleOpacity, float profilerOpacity)
        {
            if (_mainAreaStyle != null)
            {
                if (Mathf.Abs(_lastConsoleOpacity - consoleOpacity) > 0.01f ||
                    Mathf.Abs(_lastProfilerOpacity - profilerOpacity) > 0.01f)
                {
                    DestroyTextures();
                    _mainAreaStyle = null;
                }
                else
                {
                    return;
                }
            }

            _lastConsoleOpacity = consoleOpacity;
            _lastProfilerOpacity = profilerOpacity;

            _selectedBgTexture = CreateSolidColorTexture(2, 2, new Color(0.17f, 0.36f, 0.53f, consoleOpacity));
            _normalBgTexture = CreateSolidColorTexture(2, 2, new Color(0.16f, 0.16f, 0.16f, consoleOpacity * 0.9f));
            _panelBgTexture = CreateSolidColorTexture(2, 2, new Color(0.2f, 0.2f, 0.2f, consoleOpacity));
            _promptBgTexture = CreateSolidColorTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, consoleOpacity));

            _mainAreaStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelBgTexture },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(5, 5, 5, 5)
            };

            _toolbarStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(5, 5, 5, 5),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = _panelBgTexture }
            };

            _toolbarButtonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 };
            _searchFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 14, alignment = TextAnchor.MiddleLeft };

            _logItemStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                richText = true,
                fontSize = 13,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(10, 5, 5, 5),
                normal = { background = _normalBgTexture, textColor = Color.white }
            };

            _logItemSelectedStyle = new GUIStyle(_logItemStyle)
            {
                normal = { background = _selectedBgTexture, textColor = Color.white }
            };

            _detailBoxStyle = new GUIStyle(GUI.skin.box)
            {
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = _normalBgTexture }
            };

            _cancelButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _promptLabelStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _promptBgTexture, textColor = new Color(0.7f, 0.7f, 0.7f) },
                margin = new RectOffset(0, 5, 5, 0)
            };

            _historyButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _profilerStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                normal = { background = CreateSolidColorTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, profilerOpacity)) },
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        private static Texture2D CreateSolidColorTexture(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (var i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        public void DestroyTextures()
        {
            if (_selectedBgTexture != null) UnityEngine.Object.Destroy(_selectedBgTexture);
            if (_normalBgTexture != null) UnityEngine.Object.Destroy(_normalBgTexture);
            if (_panelBgTexture != null) UnityEngine.Object.Destroy(_panelBgTexture);
            if (_promptBgTexture != null) UnityEngine.Object.Destroy(_promptBgTexture);
        }

        public void DrawMiniProfiler(float fps, float ramMB, float virtualWidth, float virtualHeight)
        {
            var fpsColor = fps >= 55f ? "#00FF00" : (fps >= 30f ? "#FFFF00" : "#FF0000");
            var ramColor = ramMB < 300f ? "#FFFFFF" : (ramMB < 600f ? "#FFFF00" : "#FF0000");

            var text = $"⏱️ FPS: <color={fpsColor}>{fps:0.0}</color>   |   💾 RAM: <color={ramColor}>{ramMB:0.0} MB</color>";
            var content = new GUIContent(text);
            var size = _profilerStyle.CalcSize(content);

            if (_profilerPosX < 0)
            {
                _profilerPosX = (virtualWidth - size.x) / 2f;
                _profilerPosY = 5f;
            }

            var rect = new Rect(_profilerPosX, _profilerPosY, size.x, size.y);

            var e = Event.current;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (rect.Contains(e.mousePosition) && e.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        _isDraggingProfiler = true;
                        _profilerDragOffset = e.mousePosition - rect.position;
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID && _isDraggingProfiler)
                    {
                        _profilerPosX = e.mousePosition.x - _profilerDragOffset.x;
                        _profilerPosY = e.mousePosition.y - _profilerDragOffset.y;
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && _isDraggingProfiler)
                    {
                        GUIUtility.hotControl = 0;
                        _isDraggingProfiler = false;

                        PlayerPrefs.SetFloat(k_PrefProfilerX, _profilerPosX);
                        PlayerPrefs.SetFloat(k_PrefProfilerY, _profilerPosY);
                        PlayerPrefs.Save();
                        e.Use();
                    }

                    break;
            }

            _profilerPosX = Mathf.Clamp(_profilerPosX, 0, virtualWidth - size.x);
            _profilerPosY = Mathf.Clamp(_profilerPosY, 0, virtualHeight - size.y);
            rect.position = new Vector2(_profilerPosX, _profilerPosY);

            GUI.Label(rect, text, _profilerStyle);
        }

        public void DrawConsole(ConsoleMemory memory, ConsoleCommandProcessor command, float virtualWidth, float virtualHeight)
        {
            var mainRect = new Rect(0, 0, virtualWidth, virtualHeight);

            GUILayout.BeginArea(mainRect, _mainAreaStyle);
            GUILayout.BeginVertical();

            DrawToolbar(memory);
            DrawLogList(memory);

            if (ShowSuggestions) DrawCommandSuggestions(command, virtualHeight);
            else DrawLogDetail(virtualHeight);

            DrawCommandInputBar(command);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawToolbar(ConsoleMemory memory)
        {
            GUILayout.BeginVertical(_toolbarStyle);

            GUILayout.BeginHorizontal(GUILayout.Height(30));
            if (GUILayout.Button("Clear", _toolbarButtonStyle, GUILayout.Width(60), GUILayout.Height(25)))
            {
                memory.Clear();
                SelectedLog = null;
            }

            GUILayout.Space(10);
            GUILayout.Label("🔍 Log:", GUILayout.Width(50), GUILayout.Height(25));
            SearchString = GUILayout.TextField(SearchString, _searchFieldStyle, GUILayout.Width(130), GUILayout.Height(25));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", _cancelButtonStyle, GUILayout.Width(35), GUILayout.Height(25))) OnCloseRequested?.Invoke();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Height(30));

            var newCollapsed = GUILayout.Toggle(IsCollapsed, "Collapse", _toolbarButtonStyle, GUILayout.Width(80), GUILayout.Height(25));
            if (newCollapsed != IsCollapsed)
            {
                IsCollapsed = newCollapsed;
                SavePreference(k_PrefIsCollapsed, IsCollapsed);
            }

            GUILayout.FlexibleSpace();

            var newShowCmd = GUILayout.Toggle(ShowCommand, $"▶ {memory.CommandCount}", _toolbarButtonStyle, GUILayout.Width(60), GUILayout.Height(25));
            if (newShowCmd != ShowCommand)
            {
                ShowCommand = newShowCmd;
                SavePreference(k_PrefShowCommand, ShowCommand);
            }

            var newShowLog = GUILayout.Toggle(ShowLog, $"💬 {memory.InfoCount}", _toolbarButtonStyle, GUILayout.Width(60), GUILayout.Height(25));
            if (newShowLog != ShowLog)
            {
                ShowLog = newShowLog;
                SavePreference(k_PrefShowLog, ShowLog);
            }

            var newShowWarn = GUILayout.Toggle(ShowWarning, $"⚠️ {memory.WarnCount}", _toolbarButtonStyle, GUILayout.Width(60), GUILayout.Height(25));
            if (newShowWarn != ShowWarning)
            {
                ShowWarning = newShowWarn;
                SavePreference(k_PrefShowWarning, ShowWarning);
            }

            var newShowErr = GUILayout.Toggle(ShowError, $"🛑 {memory.ErrorCount}", _toolbarButtonStyle, GUILayout.Width(60), GUILayout.Height(25));
            if (newShowErr != ShowError)
            {
                ShowError = newShowErr;
                SavePreference(k_PrefShowError, ShowError);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawLogList(ConsoleMemory memory)
        {
            LogScrollPos = GUILayout.BeginScrollView(LogScrollPos, GUI.skin.box, GUILayout.ExpandHeight(true));

            for (var i = 0; i < memory.LogCount; i++)
            {
                var index = (memory.LogHead - memory.LogCount + i + memory.MaxLogs) % memory.MaxLogs;
                var log = memory.GetLog(index);

                if (log == null || log.CollapseCount == 0) continue;

                if (!ShowCommand && log.IsCommandEcho) continue;
                if (!ShowLog && !log.IsCommandEcho && log.Type == LogType.Log) continue;
                if (!ShowWarning && log.Type == LogType.Warning) continue;
                if (!ShowError && (log.Type == LogType.Error || log.Type == LogType.Exception)) continue;

                if (!string.IsNullOrEmpty(SearchString) && log.Condition.IndexOf(SearchString, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var isSelected = SelectedLog == log;
                var styleToUse = isSelected ? _logItemSelectedStyle : _logItemStyle;

                var iconColor = log.IsCommandEcho ? "#00FFFF" :
                    log.Type == LogType.Warning ? "#FFD700" :
                    log.Type == LogType.Error || log.Type == LogType.Exception ? "#FF4500" : "#FFFFFF";

                var iconType = log.IsCommandEcho ? "▶" :
                    log.Type == LogType.Warning ? "⚠️" :
                    log.Type == LogType.Error || log.Type == LogType.Exception ? "🛑" : "💬";

                var collapseText = IsCollapsed && log.CollapseCount > 1 ? $"<b>[{log.CollapseCount}]</b> " : "";

                var logLine = string.IsNullOrEmpty(log.ShortStackTrace)
                    ? $"<color={iconColor}>{iconType}</color> {log.TimeString} {collapseText}{log.Condition.Replace("\n", " ")}"
                    : $"<color={iconColor}>{iconType}</color> {log.TimeString} {collapseText}{log.Condition.Replace("\n", " ")}\n<color=#A0A0A0>{log.ShortStackTrace}</color>";

                if (GUILayout.Button(logLine, styleToUse))
                {
                    SelectedLog = log;
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawLogDetail(float virtualHeight)
        {
            DetailScrollPos = GUILayout.BeginScrollView(DetailScrollPos, _detailBoxStyle, GUILayout.Height(virtualHeight * 0.35f));
            if (SelectedLog != null)
            {
                var titleColor = SelectedLog.Type == LogType.Warning ? "#FFD700" :
                    SelectedLog.Type == LogType.Error || SelectedLog.Type == LogType.Exception ? "#FF4500" : "#FFFFFF";

                var stackTraceText = string.IsNullOrEmpty(SelectedLog.StackTrace) ? string.Empty : $"\n<color=#DCDCDC>{SelectedLog.StackTrace}</color>";
                GUILayout.Label($"<color={titleColor}><b>{SelectedLog.Condition}</b></color>{stackTraceText}", _detailBoxStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawCommandSuggestions(ConsoleCommandProcessor command, float virtualHeight)
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(virtualHeight * 0.35f));

            SuggestionScrollPos = GUILayout.BeginScrollView(SuggestionScrollPos);
            if (command.PendingCommand == null)
            {
                var filteredCommands = command.GetAllCommands();

                if (!string.IsNullOrEmpty(ConsoleInput))
                    filteredCommands = filteredCommands.Where(c => c.Command.Contains(ConsoleInput.ToLower()));

                if (!string.IsNullOrEmpty(CommandSearchString))
                    filteredCommands = filteredCommands.Where(c =>
                        c.Command.Contains(CommandSearchString.ToLower()) || c.Description.ToLower().Contains(CommandSearchString.ToLower()));

                foreach (var cmd in filteredCommands)
                {
                    if (GUILayout.Button($"{cmd.TemplateUsage} - <i>{cmd.Description}</i>", _logItemStyle))
                    {
                        ShowSuggestions = false;
                        SaveToHistory(cmd.Command, command);
                        OnSubmitCommand?.Invoke(cmd.Command);
                        GUI.FocusControl("ConsoleInput");
                    }
                }
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal(GUILayout.Height(30));
            GUILayout.Label("🔍 Cmd:", GUILayout.Width(55), GUILayout.Height(25));
            CommandSearchString = GUILayout.TextField(CommandSearchString, _searchFieldStyle, GUILayout.Height(25));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawCommandInputBar(ConsoleCommandProcessor command)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(40));

            if (GUILayout.Button("☰ Cmds", _toolbarButtonStyle, GUILayout.Width(70), GUILayout.Height(35)))
            {
                ShowSuggestions = !ShowSuggestions;
            }

            GUI.SetNextControlName("ConsoleInput");

            var promptText = " >";
            if (command.PendingCommand != null)
            {
                var cmdName = command.PendingCommand.CommandInfo.Command;
                if (cmdName.Length > 6) cmdName = cmdName.Substring(0, 5) + "..";
                promptText = $"[{cmdName}] args >";
            }

            GUILayout.Label(promptText, _promptLabelStyle, GUILayout.Width(promptText.Length * 9), GUILayout.Height(35));

            if (command.PendingCommand == null)
            {
                GUILayout.BeginVertical(GUILayout.Width(30), GUILayout.Height(35));

                EditorGUILayout.Space(5);
                if (GUILayout.Button("▲", _historyButtonStyle, GUILayout.Width(30), GUILayout.Height(17)))
                {
                    NavigateHistory(-1);
                }

                if (GUILayout.Button("▼", _historyButtonStyle, GUILayout.Width(30), GUILayout.Height(18)))
                {
                    NavigateHistory(1);
                }

                GUILayout.EndVertical();
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown && GUI.GetNameOfFocusedControl() == "ConsoleInput")
            {
                if (currentEvent.keyCode == KeyCode.UpArrow)
                {
                    NavigateHistory(-1);
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.DownArrow)
                {
                    NavigateHistory(1);
                    currentEvent.Use();
                }
            }

            ConsoleInput = GUILayout.TextField(ConsoleInput, _searchFieldStyle, GUILayout.Height(35));

            var isEnterPressed = currentEvent.type == EventType.KeyUp && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);

            if (GUILayout.Button("Submit", _toolbarButtonStyle, GUILayout.Width(70), GUILayout.Height(35)) || isEnterPressed)
            {
                if (!string.IsNullOrWhiteSpace(ConsoleInput))
                {
                    SaveToHistory(ConsoleInput, command);
                }

                OnSubmitCommand?.Invoke(ConsoleInput);
                ConsoleInput = "";
                _currentHistoryViewIndex = -1;
                ShowSuggestions = false;
                GUI.FocusControl("ConsoleInput");
            }

            if (command.PendingCommand != null)
            {
                if (GUILayout.Button("X", _cancelButtonStyle, GUILayout.Width(35), GUILayout.Height(35)))
                {
                    OnCancelCommand?.Invoke();
                    ConsoleInput = "";
                    GUI.FocusControl("ConsoleInput");
                }
            }

            GUILayout.EndHorizontal();
        }

        private void SaveToHistory(string cmd, ConsoleCommandProcessor command)
        {
            if (command.PendingCommand != null) return;

            var lastIndex = (_historyHead - 1 + _commandHistory.Length) % _commandHistory.Length;
            if (_historyCount > 0 && _commandHistory[lastIndex] == cmd) return;

            _commandHistory[_historyHead] = cmd;
            _historyHead = (_historyHead + 1) % _commandHistory.Length;
            if (_historyCount < _commandHistory.Length) _historyCount++;

            _currentHistoryViewIndex = -1;
        }

        private void NavigateHistory(int direction)
        {
            if (_historyCount == 0) return;

            if (_currentHistoryViewIndex == -1)
            {
                if (direction == -1) _currentHistoryViewIndex = _historyCount - 1;
                else return;
            }
            else
            {
                _currentHistoryViewIndex += direction;
            }

            if (_currentHistoryViewIndex < 0)
            {
                _currentHistoryViewIndex = -1;
                ConsoleInput = "";
            }
            else if (_currentHistoryViewIndex >= _historyCount)
            {
                _currentHistoryViewIndex = _historyCount - 1;
            }

            if (_currentHistoryViewIndex != -1)
            {
                var realIndex = (_historyHead - _historyCount + _currentHistoryViewIndex + _commandHistory.Length) % _commandHistory.Length;
                ConsoleInput = _commandHistory[realIndex];
            }

            var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (textEditor != null)
            {
                textEditor.SelectNone();
                textEditor.MoveLineEnd();
            }

            GUI.FocusControl("ConsoleInput");
        }
    }
}