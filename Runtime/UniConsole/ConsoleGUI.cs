using System;
using System.Linq;
using UnityEngine;

namespace UniConsole
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
        private GUIStyle _toolbarButtonSelectedStyle;
        private GUIStyle _toolbarSearchStyle;
        private GUIStyle _consoleInputStyle;
        private GUIStyle _logItemStyle;
        private GUIStyle _logItemSelectedStyle;
        private GUIStyle _detailBoxStyle;
        private GUIStyle _cancelButtonStyle;
        private GUIStyle _promptLabelTransparentStyle;
        private GUIStyle _profilerStyle;

        private float _splitRatio = 0.65f;
        private bool _isDraggingSplitter;

        private Texture2D _selectedBgTexture;
        private Texture2D _normalBgTexture;
        private Texture2D _panelBgTexture;
        private Texture2D _promptBgTexture;
        private Texture2D _buttonBgTexture;
        private Texture2D _buttonHoverTexture;

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
        private bool _shouldFocusInput;
        
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

            var baseColor = new Color(0.12f, 0.12f, 0.12f, consoleOpacity);
            var panelColor = new Color(0.15f, 0.15f, 0.15f, consoleOpacity);
            var buttonColor = new Color(0.2f, 0.2f, 0.2f, consoleOpacity);
            var hoverColor = new Color(0.26f, 0.26f, 0.26f, consoleOpacity);
            var accentColor = new Color(0.17f, 0.36f, 0.53f, consoleOpacity);

            _selectedBgTexture = CreateSolidColorTexture(2, 2, accentColor);
            _normalBgTexture = CreateSolidColorTexture(2, 2, baseColor);
            _panelBgTexture = CreateSolidColorTexture(2, 2, panelColor);
            _promptBgTexture = CreateSolidColorTexture(2, 2, new Color(0.08f, 0.08f, 0.08f, consoleOpacity));
            _buttonBgTexture = CreateSolidColorTexture(2, 2, buttonColor);
            _buttonHoverTexture = CreateSolidColorTexture(2, 2, hoverColor);

            _mainAreaStyle = new GUIStyle()
            {
                normal = { background = _panelBgTexture },
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 2, 2, 2)
            };

            _toolbarStyle = new GUIStyle()
            {
                padding = new RectOffset(5, 5, 5, 5),
                margin = new RectOffset(0, 0, 0, 0),
                normal = { background = _panelBgTexture }
            };

            _toolbarButtonStyle = new GUIStyle()
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _buttonBgTexture, textColor = new Color(0.9f, 0.9f, 0.9f) },
                hover = { background = _buttonHoverTexture, textColor = Color.white },
                active = { background = _selectedBgTexture, textColor = Color.white },
                onNormal = { background = _selectedBgTexture, textColor = Color.white },
                onHover = { background = _selectedBgTexture, textColor = Color.white },
                onActive = { background = _selectedBgTexture, textColor = Color.white },
                padding = new RectOffset(5, 5, 2, 2),
                margin = new RectOffset(2, 2, 2, 2)
            };

            _toolbarButtonSelectedStyle = new GUIStyle(_toolbarButtonStyle)
            {
                normal = { background = _selectedBgTexture, textColor = Color.white },
                hover = { background = _selectedBgTexture, textColor = Color.white }
            };

            _toolbarSearchStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { background = _promptBgTexture, textColor = Color.white },
                padding = new RectOffset(5, 5, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _consoleInputStyle = new GUIStyle(_toolbarSearchStyle)
            {
                margin = new RectOffset(0, 0, 2, 0)
            };

            _logItemStyle = new GUIStyle()
            {
                wordWrap = false,
                richText = true,
                fontSize = 13,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(10, 5, 5, 5),
                normal = { background = _normalBgTexture, textColor = new Color(0.85f, 0.85f, 0.85f) },
                hover = { background = _buttonHoverTexture }
            };

            _logItemSelectedStyle = new GUIStyle(_logItemStyle)
            {
                normal = { background = _selectedBgTexture, textColor = Color.white }
            };

            _detailBoxStyle = new GUIStyle()
            {
                wordWrap = true,
                richText = true,
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(10, 10, 10, 10),
                normal = { background = _normalBgTexture }
            };

            _cancelButtonStyle = new GUIStyle(_toolbarButtonStyle)
            {
                normal = { background = _buttonBgTexture, textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _promptLabelTransparentStyle = new GUIStyle()
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.5f, 0.8f, 0.9f, consoleOpacity) },
                margin = new RectOffset(8, 5, 20, 0)
            };

            _profilerStyle = new GUIStyle()
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
            if (_buttonBgTexture != null) UnityEngine.Object.Destroy(_buttonBgTexture);
            if (_buttonHoverTexture != null) UnityEngine.Object.Destroy(_buttonHoverTexture);
        }

        public void DrawMiniProfiler(float fps, float ramMB, float virtualWidth, float virtualHeight)
        {
            var fpsColor = fps >= 55f ? "#4CAF50" : (fps >= 30f ? "#FFC107" : "#F44336");
            var ramColor = ramMB < 300f ? "#E0E0E0" : (ramMB < 600f ? "#FFC107" : "#F44336");

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

            var availableHeight = virtualHeight - 124f;

            var listHeight = Mathf.Max(20f, availableHeight * _splitRatio);
            var detailHeight = Mathf.Max(20f, availableHeight * (1f - _splitRatio));

            DrawLogList(memory, listHeight);

            DrawSplitter(virtualWidth);

            if (ShowSuggestions) DrawCommandSuggestions(command, detailHeight);
            else DrawLogDetail(detailHeight);

            DrawCommandInputBar(command);

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawSplitter(float virtualWidth)
        {
            var splitterRect = GUILayoutUtility.GetRect(virtualWidth, 5f);
            GUI.DrawTexture(splitterRect, _buttonHoverTexture);

#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.AddCursorRect(splitterRect, UnityEditor.MouseCursor.ResizeVertical);
#endif

            var e = Event.current;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(e.mousePosition) && e.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        _isDraggingSplitter = true;
                        e.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID && _isDraggingSplitter)
                    {
                        var availableHeight = Screen.height / GUI.matrix.m00 - 124f;
                        var newY = e.mousePosition.y - 72f;

                        _splitRatio = Mathf.Clamp(newY / availableHeight, 0.15f, 0.85f);
                        e.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID && _isDraggingSplitter)
                    {
                        GUIUtility.hotControl = 0;
                        _isDraggingSplitter = false;
                        e.Use();
                    }

                    break;
            }
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

            GUI.SetNextControlName("LogSearchBox");
            SearchString = GUILayout.TextField(SearchString, _toolbarSearchStyle, GUILayout.Width(200), GUILayout.Height(25));

            if (string.IsNullOrEmpty(SearchString) && GUI.GetNameOfFocusedControl() != "LogSearchBox")
            {
                var rect = GUILayoutUtility.GetLastRect();
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(rect, " 🔍 Search Log...", _toolbarSearchStyle);
                GUI.color = prevColor;
            }

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

        private void DrawLogList(ConsoleMemory memory, float listHeight)
        {
            LogScrollPos = GUILayout.BeginScrollView(LogScrollPos, GUILayout.Height(listHeight));

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

                var iconColor = log.IsCommandEcho ? "#4FC3F7" :
                    log.Type == LogType.Warning ? "#FFD54F" :
                    log.Type == LogType.Error || log.Type == LogType.Exception ? "#EF5350" : "#E0E0E0";

                var iconType = log.IsCommandEcho ? "▶" :
                    log.Type == LogType.Warning ? "⚠️" :
                    log.Type == LogType.Error || log.Type == LogType.Exception ? "🛑" : "💬";

                var collapseText = IsCollapsed && log.CollapseCount > 1 ? $"<b>[{log.CollapseCount}]</b> " : "";

                var logText = string.IsNullOrEmpty(log.ShortStackTrace)
                    ? $"<color={iconColor}>{iconType}</color> {collapseText}{log.Condition.Replace("\n", " ")}"
                    : $"<color={iconColor}>{iconType}</color> {collapseText}{log.Condition.Replace("\n", " ")}\n<color=#888888>{log.ShortStackTrace}</color>";

                var logRect = GUILayoutUtility.GetRect(new GUIContent(logText), styleToUse, GUILayout.ExpandWidth(true));

                if (GUI.Button(logRect, "", styleToUse))
                {
                    SelectedLog = log;
                }

                var textRect = new Rect(logRect.x, logRect.y, logRect.width - 75, logRect.height);
                GUI.Label(textRect, logText, styleToUse);

                var timeStyle = new GUIStyle(styleToUse) { alignment = TextAnchor.UpperRight, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
                var timeDisplayRect = new Rect(logRect.x + logRect.width - 70, logRect.y, 65, logRect.height);
                GUI.Label(timeDisplayRect, log.TimeString, timeStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawLogDetail(float detailHeight)
        {
            GUILayout.BeginVertical(_detailBoxStyle, GUILayout.Height(detailHeight));

            DetailScrollPos = GUILayout.BeginScrollView(DetailScrollPos, GUIStyle.none, GUILayout.ExpandHeight(true));

            if (SelectedLog != null)
            {
                var titleColor = SelectedLog.Type == LogType.Warning ? "#FFD54F" :
                    SelectedLog.Type == LogType.Error || SelectedLog.Type == LogType.Exception ? "#EF5350" : "#FFFFFF";

                var stackTraceText = string.IsNullOrEmpty(SelectedLog.StackTrace) ? string.Empty : $"\n<color=#888888>{SelectedLog.StackTrace}</color>";

                var textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 13 };
                GUILayout.Label($"<color={titleColor}><b>{SelectedLog.Condition}</b></color>{stackTraceText}", textStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawCommandSuggestions(ConsoleCommandProcessor command, float detailHeight)
        {
            GUILayout.BeginVertical(_detailBoxStyle, GUILayout.Height(detailHeight));

            SuggestionScrollPos = GUILayout.BeginScrollView(SuggestionScrollPos, GUIStyle.none, GUILayout.ExpandHeight(true));

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
                    var parameters = string.Join(" ", cmd.Parameters.Select(p => $"<color=#81C784><{p.ParameterType.Name}</color> <color=#B0BEC5>{p.Name}></color>"));
                    var usage = $"<b><color=#4FC3F7>{cmd.Command}</color></b> {parameters}";
                    var description = $"<color=#888888><i>- {cmd.Description}</i></color>";

                    if (GUILayout.Button($"{usage} {description}", _logItemStyle))
                    {
                        ShowSuggestions = false;
                        SaveToHistory(cmd.Command, command);
                        OnSubmitCommand?.Invoke(cmd.Command);
                        
                        if (command.PendingCommand != null)
                        {
                            _shouldFocusInput = true; 
                        }
                        else
                        {
                            GUI.FocusControl(null); 
                        }
                    }
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal(GUIStyle.none, GUILayout.Height(25));

            GUI.SetNextControlName("CmdSearchBox");
            CommandSearchString = GUILayout.TextField(CommandSearchString, _toolbarSearchStyle, GUILayout.Height(25));

            if (string.IsNullOrEmpty(CommandSearchString) && GUI.GetNameOfFocusedControl() != "CmdSearchBox")
            {
                var rect = GUILayoutUtility.GetLastRect();
                var prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(rect, " 🔍 Find Command...", _toolbarSearchStyle);
                GUI.color = prevColor;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawCommandInputBar(ConsoleCommandProcessor command)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(45));

            var cmdBtnStyle = ShowSuggestions ? _toolbarButtonSelectedStyle : _toolbarButtonStyle;
            if (GUILayout.Button(">_", cmdBtnStyle, GUILayout.Width(40), GUILayout.Height(35)))
            {
                ShowSuggestions = !ShowSuggestions;
            }

            GUI.SetNextControlName("ConsoleInput");

            var promptText = ">";
            if (command.PendingCommand != null)
            {
                var cmdName = command.PendingCommand.CommandInfo.Command;
                if (cmdName.Length > 6) cmdName = cmdName.Substring(0, 5) + "..";
                promptText = $"[{cmdName}] >";
            }

            GUILayout.Label(promptText, _promptLabelTransparentStyle, GUILayout.Width(promptText.Length * 10));

            ConsoleInput = GUILayout.TextField(ConsoleInput, _consoleInputStyle, GUILayout.Height(35));

            if (_shouldFocusInput)
            {
                GUI.FocusControl("ConsoleInput");
                if (Event.current.type == EventType.Repaint)
                {
                    _shouldFocusInput = false;
                    var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    if (textEditor != null)
                    {
                        textEditor.SelectNone();
                        textEditor.MoveLineEnd();
                    }
                }
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

            var isEnterPressed = currentEvent.type == EventType.KeyUp && (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter);
            var shouldSubmit = isEnterPressed;

            if (command.PendingCommand == null)
            {
                if (GUILayout.Button("🕒", _toolbarButtonStyle, GUILayout.Width(40), GUILayout.Height(35)))
                {
                    NavigateHistory(-1);
                }
            }
            else
            {
                if (GUILayout.Button("✖", _cancelButtonStyle, GUILayout.Width(40), GUILayout.Height(35)))
                {
                    OnCancelCommand?.Invoke();
                    ConsoleInput = "";
                    GUI.FocusControl(null); 
                }
            }

            if (shouldSubmit)
            {
                Event.current.Use();

                if (!string.IsNullOrWhiteSpace(ConsoleInput))
                {
                    SaveToHistory(ConsoleInput, command);
                }

                OnSubmitCommand?.Invoke(ConsoleInput);
                ConsoleInput = "";
                _currentHistoryViewIndex = -1;
                ShowSuggestions = false;

                if (command.PendingCommand != null)
                {
                    _shouldFocusInput = true;
                }
                else
                {
                    GUI.FocusControl(null);
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