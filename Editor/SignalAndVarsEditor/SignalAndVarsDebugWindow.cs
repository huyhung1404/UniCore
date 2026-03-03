using System;
using System.Collections.Generic;
using System.Reflection;
using UniCore.Signal;
using UniCore.Vars;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.Windows
{
    public class SignalAndVarsDebugWindow : EditorWindow
    {
        private const float k_minPanelRatio = 0.15f;
        private const float k_maxPanelRatio = 0.85f;
        private Vector2 _scroll;
        private Vector2 _varScroll;
        private double _lastRepaint;
        private string _sceneFilter = string.Empty;
        private string _scopeFilter = string.Empty;
        private string _signalFilter = string.Empty;
        private string _listenerFilter = string.Empty;
        private bool _autoRefresh;
        private bool _drawVariable;
        private readonly Dictionary<Type, bool> _foldouts = new Dictionary<Type, bool>(64);
        private static GUIContent s_pingIcon;
        private static GUIContent s_deleteIcon;
        private static GUIContent s_sendIcon;
        private static GUIStyle s_metaStyle;
        private static GUIContent s_editIcon;
        private static GUIContent s_csIcon;
        private float _signalPanelRatio;
        private bool _isResizing;

        [MenuItem("UniCore/Windows/Signal And Vars", priority = 0)]
        public static void Open()
        {
            GetWindow<SignalAndVarsDebugWindow>("Signal & Vars");
        }

        private void OnEnable()
        {
            _autoRefresh = EditorPrefs.GetBool("UniSignal.AutoRefresh", true);
            _drawVariable = EditorPrefs.GetBool("UniSignal.DrawVariable", false);
            EditorApplication.update += UpdateLoop;
            _signalPanelRatio = EditorPrefs.GetFloat("UniSignal.SignalPanelRatio", 0.5f);
#if UNICORE_TESTING
            var shouldAutoInject = EditorPrefs.GetBool("UniSignal.AutoInjectFakeData", false);
            if (shouldAutoInject) Testing.SignalVarsMockup.InjectFakeData();
#endif
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool("UniSignal.AutoRefresh", _autoRefresh);
            EditorPrefs.SetBool("UniSignal.DrawVariable", _drawVariable);
            EditorPrefs.SetFloat("UniSignal.SignalPanelRatio", _signalPanelRatio);
            EditorApplication.update -= UpdateLoop;
        }

        private void UpdateLoop()
        {
            if (!_autoRefresh) return;
            if (!(EditorApplication.timeSinceStartup - _lastRepaint > 0.2f)) return;
            _lastRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            InitIcons();
            DrawToolbar();

            var currentSignalHeight = position.height * _signalPanelRatio;
            var signalHeightOption = _drawVariable ? GUILayout.Height(currentSignalHeight) : GUILayout.ExpandHeight(true);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, false, false, signalHeightOption);
            foreach (var kvp in SignalSystem.s_Listeners)
            {
                if (!PassSignalFilter(kvp.Key)) continue;
                DrawSignalType(kvp.Key, kvp.Value);
            }

            EditorGUILayout.EndScrollView();

            if (!_drawVariable) return;
            DrawSplitter();
            DrawVariable();
        }

        private void DrawSplitter()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.Height(4), GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        _isResizing = true;
                        Event.current.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (_isResizing)
                    {
                        var deltaRatio = Event.current.delta.y / position.height;
                        _signalPanelRatio += deltaRatio;
                        _signalPanelRatio = Mathf.Clamp(_signalPanelRatio, k_minPanelRatio, k_maxPanelRatio);

                        Event.current.Use();
                        Repaint();
                    }

                    break;
                case EventType.MouseUp:
                    if (_isResizing)
                    {
                        _isResizing = false;
                        EditorPrefs.SetFloat("UniSignal.SignalPanelRatio", _signalPanelRatio);
                        Event.current.Use();
                    }

                    break;
            }
        }

        private static void InitIcons()
        {
            if (s_pingIcon != null) return;
            s_pingIcon = EditorGUIUtility.IconContent("d_Search Icon");
            s_deleteIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
            s_sendIcon = EditorGUIUtility.IconContent("d_PlayButton");
            s_editIcon = EditorGUIUtility.IconContent("d_editicon.sml");
            s_editIcon.tooltip = "Edit Object Data";
            s_csIcon = EditorGUIUtility.IconContent("cs Script Icon");
            s_csIcon.tooltip = "Pure C# Class (Non-Mono)";
        }

        private bool PassSignalFilter(Type signalType)
        {
            return string.IsNullOrEmpty(_signalFilter) || signalType.Name.Contains(_signalFilter, StringComparison.OrdinalIgnoreCase);
        }

        #region UI Sections

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
                _drawVariable = GUILayout.Toggle(_drawVariable, "Variable", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Release Empty Lists", EditorStyles.toolbarButton)) SignalSystem.ReleaseEmptyLists();
                if (GUILayout.Button("Clear All", EditorStyles.toolbarButton)) SignalSystem.Clear();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Signal", EditorStyles.miniBoldLabel, GUILayout.MaxWidth(36));
                _signalFilter = GUILayout.TextField(_signalFilter, EditorStyles.toolbarSearchField);

                EditorGUILayout.LabelField("Listener", EditorStyles.miniBoldLabel, GUILayout.MaxWidth(45));
                _listenerFilter = GUILayout.TextField(_listenerFilter, EditorStyles.toolbarSearchField);

                EditorGUILayout.LabelField("Scene", EditorStyles.miniBoldLabel, GUILayout.MaxWidth(36));
                _sceneFilter = GUILayout.TextField(_sceneFilter, EditorStyles.toolbarSearchField);

                EditorGUILayout.LabelField("Scope", EditorStyles.miniBoldLabel, GUILayout.MaxWidth(36));
                _scopeFilter = GUILayout.TextField(_scopeFilter, EditorStyles.toolbarSearchField);
            }
        }

        private void DrawSignalType(Type signalType, IListenerList list)
        {
            _foldouts.TryAdd(signalType, true);

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(false));

            using (new EditorGUILayout.HorizontalScope())
            {
                var label = $"{signalType.Name} ({list.Count})";
                _foldouts[signalType] = EditorGUILayout.Foldout(_foldouts[signalType], label, true, EditorStyles.foldoutHeader);

                GUILayout.FlexibleSpace();

                s_sendIcon.tooltip = "Dispatch Signal";
                if (GUILayout.Button(s_sendIcon, EditorStyles.iconButton, GUILayout.Width(30)))
                {
                    SignalSendPopup.Open(signalType, GUILayoutUtility.GetLastRect());
                }
            }

            if (_foldouts[signalType])
            {
                GUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(6);

                    EditorGUILayout.BeginVertical();
                    var drawIndex = 0;

                    for (var i = 0; i < list.Count; i++)
                    {
                        var listener = list.Get(i);
                        if (!PassFilter(listener)) continue;

                        DrawListener(signalType, listener, drawIndex);
                        drawIndex++;
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private bool PassFilter(object listener)
        {
            if (!string.IsNullOrEmpty(_listenerFilter))
            {
                if (!listener.GetType().Name
                        .Contains(_listenerFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrEmpty(_sceneFilter))
            {
                if (listener is MonoBehaviour mb)
                {
                    var sceneName = mb.gameObject.scene.name;
                    if (!sceneName.Contains(_sceneFilter, StringComparison.OrdinalIgnoreCase)) return false;
                }
                else
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(_scopeFilter)) return true;
            foreach (var itf in listener.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType || itf.GetGenericTypeDefinition() != typeof(ISignalListener<>)) continue;
                var scope = itf.GetProperty("ListenScope")?.GetValue(listener);
                if (scope == null) return false;
                var value = SignalScopeRegistry.GetReadableScope((SignalScope)scope);
                if (!value.Contains(_scopeFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static void DrawListener(Type signalType, object listener, int index)
        {
            var priority = GetListenerPriority(listener);
            var priorityColor = GetPriorityColor(priority);

            var listenerName = listener.GetType().Name;
            var sourceText = SignalDebugUtil.GetSource(listener);

            var rect = EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false));

            if (Event.current.type == EventType.Repaint)
            {
                var bgColor = index % 2 == 0
                    ? new Color(0.15f, 0.15f, 0.15f, 0.2f)
                    : new Color(0.25f, 0.25f, 0.25f, 0.2f);
                EditorGUI.DrawRect(rect, bgColor);

                var colorIndicatorRect = new Rect(rect.x, rect.y, 4, rect.height);
                EditorGUI.DrawRect(colorIndicatorRect, priorityColor);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);

                var isUnityObject = SignalDebugUtil.TryGetUnityObject(listener, out var unityObj);

                if (isUnityObject)
                {
                    s_pingIcon.tooltip = "Ping Unity Object";
                    if (GUILayout.Button(s_pingIcon, EditorStyles.iconButton, GUILayout.Width(16), GUILayout.Height(16)))
                    {
                        EditorGUIUtility.PingObject(unityObj);
                        Selection.activeObject = unityObj;
                    }
                }
                else
                {
                    var oldColor = GUI.color;
                    GUI.color = new Color(1, 1, 1, 0.4f);
                    GUILayout.Label(s_csIcon, GUILayout.Width(16), GUILayout.Height(16));

                    GUI.color = oldColor;
                }

                GUILayout.Space(4);

                var headerStyle = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
                EditorGUILayout.LabelField($"<b>{listenerName}</b> <color=#909090><i>({sourceText})</i></color>", headerStyle);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(32);

                if (s_metaStyle == null)
                {
                    s_metaStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        richText = true,
                        padding = new RectOffset(0, 0, 0, 0),
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }

                var oldColor = GUI.color;
                GUI.color = priorityColor;
                GUILayout.Label($"● Priority: {priority}", s_metaStyle, GUILayout.Width(80));
                GUI.color = oldColor;

                GUILayout.Space(8);
                DrawListenerScope(listener, s_metaStyle);

                // Đẩy nút Delete dạt ra sát lề phải của hàng thứ 2
                GUILayout.FlexibleSpace();

                s_deleteIcon.tooltip = "Unregister Listener";
                if (GUILayout.Button(s_deleteIcon, EditorStyles.iconButton, GUILayout.Width(20)))
                {
                    UnregisterListener(signalType, listener);
                }
            }

            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private static void DrawListenerScope(object listener, GUIStyle style)
        {
            foreach (var itf in listener.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType || itf.GetGenericTypeDefinition() != typeof(ISignalListener<>)) continue;

                var scope = itf.GetProperty("ListenScope")?.GetValue(listener);
                var scopeText = scope != null ? SignalScopeRegistry.GetReadableScope((SignalScope)scope) : "N/A";

                GUILayout.Label($"<color=#aaaaaa>❖ Scope: {scopeText}</color>", style, GUILayout.ExpandWidth(false));

                return;
            }
        }

        private static int GetListenerPriority(object listener)
        {
            foreach (var itf in listener.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType ||
                    itf.GetGenericTypeDefinition() != typeof(ISignalListener<>))
                    continue;

                var prop = itf.GetProperty("Priority");
                if (prop != null) return (int)prop.GetValue(listener);
            }

            return 0;
        }

        private static Color GetPriorityColor(int priority)
        {
            priority = Mathf.Clamp(priority, 0, 100);

            var white = Color.white;
            var green = new Color(0.7f, 1f, 0.7f);
            var yellow = new Color(1f, 1f, 0.6f);
            var red = new Color(1f, 0.6f, 0.6f);

            switch (priority)
            {
                case <= 10:
                {
                    var t = priority / 10f;
                    return Color.Lerp(white, green, t);
                }
                case <= 50:
                {
                    var t = (priority - 10f) / 40f;
                    return Color.Lerp(green, yellow, t);
                }
                default:
                {
                    var tFinal = (priority - 50f) / 50f;
                    return Color.Lerp(yellow, red, tFinal);
                }
            }
        }

        private void DrawVariable()
        {
            GUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("Variables Overview", EditorStyles.boldLabel);
            }

            GUILayout.Space(4);

            _varScroll = EditorGUILayout.BeginScrollView(_varScroll, false, false, GUILayout.ExpandHeight(true));

            foreach (var (nameStore, store) in VarsSystem.AllStores)
            {
                DrawVariableStore(nameStore, store);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawVariableStore(string nameStore, VariableStore store)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false));

            EditorGUILayout.LabelField($"■ {nameStore}", EditorStyles.boldLabel);
            GUILayout.Space(2);

            var drawIndex = 0;
            foreach (var v in store.All)
            {
                DrawVariableItem(v, drawIndex);
                drawIndex++;
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(8);
        }

        private static void DrawVariableItem(object variable, int index)
        {
            var type = variable.GetType();

            var keyField = type.GetField("_key", BindingFlags.Instance | BindingFlags.NonPublic);
            var valueField = type.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);

            if (keyField == null || valueField == null) return;

            var key = (string)keyField.GetValue(variable);
            var value = valueField.GetValue(variable);

            var rect = EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(false));

            if (Event.current.type == EventType.Repaint)
            {
                var rowRect = new Rect(0, rect.y, EditorGUIUtility.currentViewWidth, rect.height > 0 ? rect.height : 20f);
                var bgColor = index % 2 == 0
                    ? new Color(0.15f, 0.15f, 0.15f, 0.2f)
                    : new Color(0.25f, 0.25f, 0.25f, 0.2f);
                EditorGUI.DrawRect(rowRect, bgColor);
            }

            GUILayout.Space(12);

            var safeWidth = EditorGUIUtility.currentViewWidth - 50f;
            var keyWidth = Mathf.Clamp(safeWidth * 0.4f, 100f, 200f);
            EditorGUILayout.LabelField(key, EditorStyles.boldLabel, GUILayout.Width(keyWidth));

            var newValue = DrawInlineValue(valueField.FieldType, value, v => valueField.SetValue(variable, v));

            if (!Equals(value, newValue))
            {
                valueField.SetValue(variable, newValue);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static object DrawInlineValue(Type type, object value, Action<object> onApply)
        {
            var (isDrawn, drawnValue) = EditorExtensions.DrawDefaultValue(type, GUIContent.none, value);

            if (isDrawn) return drawnValue;

            using (new EditorGUILayout.HorizontalScope())
            {
                var typeName = $"[{type.Name}]";
                EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel, GUILayout.MinWidth(20));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(s_editIcon, EditorStyles.iconButton, GUILayout.Width(20)))
                {
                    ObjectEditPopup.Open($"Edit {type.Name}", value, onApply, GUILayoutUtility.GetLastRect());
                }
            }

            return value;
        }

        #endregion

        #region Unregister

        private static void UnregisterListener(Type signalType, object listener)
        {
            var method = typeof(SignalSystem).GetMethod("Unregister")?.MakeGenericMethod(signalType);
            method?.Invoke(null, new[] { listener });
        }

        #endregion
    }
}