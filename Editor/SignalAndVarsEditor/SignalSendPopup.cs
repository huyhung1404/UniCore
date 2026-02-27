using System;
using System.Collections.Generic;
using System.Reflection;
using UniCore.Signal;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor
{
    public class SignalSendPopup : EditorWindow
    {
        private Type _signalType;
        private object _signalInstance;
        private SignalScope _scope = SignalScope.All;
        
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        public static void Open(Type signalType, Rect activatorRect)
        {
            var window = CreateInstance<SignalSendPopup>();
            window._signalType = signalType;
            window._signalInstance = Activator.CreateInstance(signalType);
            
            window.titleContent = new GUIContent($"Dispatch: {signalType.Name}");
            window.minSize = window.maxSize = new Vector2(350, 100); 
            var screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(activatorRect.x, activatorRect.yMax));
            window.position = new Rect(screenPoint.x, screenPoint.y, 350, 100);
            window.ShowUtility();
        }
        
        private void OnLostFocus()
        {
            Close();
        }

        private void OnGUI()
        {
            InitStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false);
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false));

            DrawHeader();
            
            GUILayout.Space(10);
            DrawSignalFields();

            GUILayout.Space(10);
            DrawScopeField();

            GUILayout.Space(15);
            DrawActionButtons();

            EditorGUILayout.EndVertical();
            
            if (Event.current.type == EventType.Repaint)
            {
                var contentRect = GUILayoutUtility.GetLastRect(); 
                
                var targetHeight = contentRect.height + 4f; 
                
                const float k_maxHeight = 650f;
                var finalHeight = Mathf.Min(targetHeight, k_maxHeight);

                if (Mathf.Abs(position.height - finalHeight) > 1f)
                {
                    minSize = maxSize = new Vector2(350, finalHeight);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 5)
            };
        }

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), new Color(0.1f, 0.1f, 0.1f, 1f));
            }

            EditorGUI.LabelField(rect, $"Signal: {_signalType.Name}", _headerStyle);
        }

        private void DrawSignalFields()
        {
            EditorGUILayout.LabelField("Signal Data", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("helpBox");

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var hasFields = false;

            foreach (var field in _signalType.GetFields(flags))
            {
                if (field.IsInitOnly) continue;
                hasFields = true;

                var value = field.GetValue(_signalInstance);
                var newValue = DrawValue(field.FieldType, field.Name, value, field.Name);

                if (!Equals(value, newValue))
                    field.SetValue(_signalInstance, newValue);
            }

            foreach (var prop in _signalType.GetProperties(flags))
            {
                if (!prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;
                hasFields = true;

                var value = prop.GetValue(_signalInstance);
                var newValue = DrawValue(prop.PropertyType, prop.Name, value, prop.Name);

                if (!Equals(value, newValue))
                    prop.SetValue(_signalInstance, newValue);
            }

            if (!hasFields)
            {
                EditorGUILayout.LabelField("No public mutable fields/properties found.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private object DrawValue(Type type, string label, object value, string path)
        {
            var v = EditorExtensions.DrawDefaultValue(type, new GUIContent(label), value);
            if (v.Item1) return v.Item2;

            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                EditorGUILayout.LabelField($"[Unsupported Collection: {type.Name}]", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                return value;
            }

            if (value == null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(label);
                if (GUILayout.Button($"Create {type.Name}", EditorStyles.miniButton))
                {
                    try { value = Activator.CreateInstance(type); }
                    catch
                    {
                        // ignored
                    }
                }
                EditorGUILayout.EndHorizontal();
                return value;
            }

            var foldoutKey = string.IsNullOrEmpty(path) ? label : $"{path}.{label}";
            _foldouts.TryAdd(foldoutKey, true);

            EditorGUILayout.BeginVertical("box");
            _foldouts[foldoutKey] = EditorGUILayout.Foldout(_foldouts[foldoutKey], label, true, EditorStyles.foldoutHeader);

            if (_foldouts[foldoutKey])
            {
                EditorGUI.indentLevel++;
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

                var boxedValue = value;

                foreach (var field in type.GetFields(flags))
                {
                    if (field.IsInitOnly) continue;
                    var fValue = field.GetValue(boxedValue);
                    
                    var newFValue = DrawValue(field.FieldType, field.Name, fValue, foldoutKey);
                    
                    if (!Equals(fValue, newFValue))
                        field.SetValue(boxedValue, newFValue);
                }

                foreach (var prop in type.GetProperties(flags))
                {
                    if (!prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;
                    var pValue = prop.GetValue(boxedValue);
                    
                    var newPValue = DrawValue(prop.PropertyType, prop.Name, pValue, foldoutKey);
                    
                    if (!Equals(pValue, newPValue))
                        prop.SetValue(boxedValue, newPValue);
                }
                
                value = boxedValue;
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();

            return value;
        }

        private void DrawScopeField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Target Scope", EditorStyles.boldLabel, GUILayout.Width(120));
                
                var scopeText = SignalScopeRegistry.GetReadableScope(_scope);
                EditorGUILayout.LabelField(scopeText, EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginVertical("helpBox");

            var entries = SignalScopeRegistry.scopeNames;
            var currentMask = _scope.Mask;

            var currentColumn = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var kvp in entries)
            {
                var mask = kvp.Key;
                var vName = kvp.Value;

                if (currentColumn >= 2)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentColumn = 0;
                }

                if (mask == ulong.MaxValue)
                {
                    var isAll = currentMask == ulong.MaxValue;
                    var newAll = EditorGUILayout.ToggleLeft("★ " + vName, isAll, GUILayout.Width(150));

                    if (newAll && !isAll) currentMask = ulong.MaxValue;
                    else if (!newAll && isAll) currentMask = 0;
                }
                else
                {
                    var hasFlag = (currentMask & mask) != 0;
                    var newHasFlag = EditorGUILayout.ToggleLeft(vName, hasFlag, GUILayout.Width(150));

                    if (newHasFlag != hasFlag)
                    {
                        if (currentMask == ulong.MaxValue) currentMask = 0; 
                        
                        if (newHasFlag) currentMask |= mask;
                        else currentMask &= ~mask;
                    }
                }

                currentColumn++;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            
            _scope = currentMask == 0 ? new SignalScope(0) : new SignalScope(currentMask);
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.FlexibleSpace();

            var oldColor = GUI.color;
            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            
            if (GUILayout.Button("Dispatch Signal", EditorStyles.miniButton, GUILayout.Width(200), GUILayout.Height(30)))
            {
                Send();
                Close();
            }
            
            GUI.color = oldColor;
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private void Send()
        {
            var method = typeof(SignalSystem)
                .GetMethod("Dispatch", new[] { _signalType, typeof(SignalScope) })
                ?.MakeGenericMethod(_signalType);

            method?.Invoke(null, new[] { _signalInstance, _scope });
        }
    }
}