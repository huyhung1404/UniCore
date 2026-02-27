using System;
using System.Collections.Generic;
using System.Reflection;
using UniCore.Signal;
using UniCore.Vars;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor
{
    public class SignalAndVarsDebugWindow : EditorWindow
    {
        private Vector2 _scroll;
        private Vector2 _historyScroll;
        private double _lastRepaint;
        private string _sceneFilter = string.Empty;
        private string _scopeFilter = string.Empty;
        private string _signalFilter = string.Empty;
        private string _listenerFilter = string.Empty;
        private bool _autoRefresh;
        private bool _drawHistory;
        private bool _drawVariable;
        private readonly Dictionary<Type, bool> _foldouts = new Dictionary<Type, bool>(64);

        [MenuItem("UniCore/Windows/Signal And Vars", priority = 0)]
        public static void Open()
        {
            GetWindow<SignalAndVarsDebugWindow>("Signal & Vars");
        }

        private void OnEnable()
        {
            _autoRefresh = EditorPrefs.GetBool("UniSignal.AutoRefresh", true);
            _drawHistory = EditorPrefs.GetBool("UniSignal.DrawHistory", false);
            _drawVariable = EditorPrefs.GetBool("UniSignal.DrawVariable", false);
            EditorApplication.update += UpdateLoop;
        }

        private void OnDisable()
        {
            EditorPrefs.SetBool("UniSignal.AutoRefresh", _autoRefresh);
            EditorPrefs.SetBool("UniSignal.DrawHistory", _drawHistory);
            EditorPrefs.SetBool("UniSignal.DrawVariable", _drawVariable);
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
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var kvp in SignalSystem.s_Listeners)
            {
                if (!PassSignalFilter(kvp.Key)) continue;
                DrawSignalType(kvp.Key, kvp.Value);
            }

            EditorGUILayout.EndScrollView();

            if (_drawVariable) DrawVariable();
            if (_drawHistory) DrawDispatchHistory();
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
                _drawHistory = GUILayout.Toggle(_drawHistory, "History", EditorStyles.toolbarButton);
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

            EditorGUILayout.BeginVertical("box");

            using (new EditorGUILayout.HorizontalScope())
            {
                _foldouts[signalType] = EditorGUILayout.Foldout(_foldouts[signalType], $"{signalType.Name} ({list.Count})", true);

                if (GUILayout.Button("Send", GUILayout.Width(60)))
                {
                    SignalSendPopup.Open(signalType);
                }
            }

            if (_foldouts[signalType])
            {
                EditorGUI.indentLevel++;
                for (var i = 0; i < list.Count; i++)
                {
                    if (!PassFilter(list.Get(i))) continue;
                    DrawListener(signalType, list.Get(i));
                }

                EditorGUI.indentLevel--;
            }

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

        private static void DrawListener(Type signalType, object listener)
        {
            var priority = GetListenerPriority(listener);
            var oldColor = GUI.color;
            GUI.color = GetPriorityColor(priority);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(listener.GetType().Name, GUILayout.Width(150)))
                {
                    if (SignalDebugUtil.TryGetUnityObject(listener, out var obj))
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }

                EditorGUILayout.LabelField(SignalDebugUtil.GetSource(listener), GUILayout.Width(80));

                DrawListenerMeta(listener);

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.Width(EditorGUIUtility.singleLineHeight)))
                {
                    UnregisterListener(signalType, listener);
                }
            }

            GUI.color = oldColor;
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

        private static void DrawListenerMeta(object listener)
        {
            foreach (var itf in listener.GetType().GetInterfaces())
            {
                if (!itf.IsGenericType || itf.GetGenericTypeDefinition() != typeof(ISignalListener<>)) continue;
                var priority = itf.GetProperty("Priority")?.GetValue(listener);
                var scope = itf.GetProperty("ListenScope")?.GetValue(listener);
                var scopeText = scope != null ? SignalScopeRegistry.GetReadableScope((SignalScope)scope) : string.Empty;
                EditorGUILayout.LabelField($"P: {priority} | S: {scopeText}");
            }
        }

        private void DrawDispatchHistory()
        {
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Dispatch History", EditorStyles.boldLabel);

            _historyScroll = EditorGUILayout.BeginScrollView(_historyScroll, GUILayout.Height(120));
            foreach (var record in SignalDispatchHistory.Records) EditorGUILayout.LabelField(record);
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Clear History")) SignalDispatchHistory.Clear();
        }

        private void DrawVariable()
        {
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("Variables", EditorStyles.boldLabel);
            foreach (var (nameStore, store) in VarsSystem.AllStores)
            {
                DrawVariableStore(nameStore, store);
            }
        }

        private void DrawVariableStore(string nameStore, VariableStore store)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(nameStore, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var v in store.All) DrawVariable(v);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private static void DrawVariable(object variable)
        {
            var type = variable.GetType();
            var key = (string)type.GetField("_key", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(variable);
            var valueProp = type.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(key, GUILayout.Width(180));

            var value = valueProp!.GetValue(variable);
            var newValue = DrawInlineValue(
                valueProp.FieldType,
                value,
                v => valueProp.SetValue(variable, v)
            );

            if (!Equals(value, newValue))
            {
                valueProp.SetValue(variable, newValue);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static object DrawInlineValue(Type type, object value, Action<object> onApply)
        {
            var v = EditorExtensions.DrawDefaultValue(type, GUIContent.none, value);
            if (v.Item1) return v.Item2;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(type.Name, GUILayout.MaxWidth(120));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Edit", GUILayout.Width(40)))
            {
                ObjectEditPopup.Open($"Edit {type.Name}", value, onApply);
            }

            EditorGUILayout.EndHorizontal();

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