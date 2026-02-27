using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor
{
    public class ObjectEditPopup : EditorWindow
    {
        private object _target;
        private Type _targetType;
        private Action<object> _onApply;

        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        
        private readonly Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();

        public static void Open(string title, object value, Action<object> onApply, Rect activatorRect)
        {
            var window = CreateInstance<ObjectEditPopup>();
            if (value != null)
            {
                window._target = Clone(value);
                window._targetType = value.GetType();
            }

            window._onApply = onApply;
            window.titleContent = new GUIContent(title);
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
            if (_target == null)
            {
                Close();
                return;
            }

            InitStyles();
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, false);
            
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false));

            DrawHeader();
            
            GUILayout.Space(10);
            DrawFields();

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

            EditorGUI.LabelField(rect, $"Edit: {_targetType.Name}", _headerStyle);
        }

        private void DrawFields()
        {
            EditorGUILayout.LabelField("Object Data", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("helpBox");

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            var hasFields = false;

            foreach (var field in _targetType.GetFields(flags))
            {
                if (field.IsInitOnly) continue;
                hasFields = true;

                var value = field.GetValue(_target);
                var newValue = DrawValue(field.FieldType, field.Name, value, field.Name);

                if (!Equals(value, newValue))
                    field.SetValue(_target, newValue);
            }

            foreach (var prop in _targetType.GetProperties(flags))
            {
                if (!prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;
                hasFields = true;

                var value = prop.GetValue(_target);
                var newValue = DrawValue(prop.PropertyType, prop.Name, value, prop.Name);

                if (!Equals(value, newValue))
                    prop.SetValue(_target, newValue);
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
                    
                    var newFValue = DrawValue(field.FieldType, field.Name, fValue, foldoutKey); // Gọi đệ quy
                    
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

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var oldColor = GUI.color;
            GUI.color = new Color(0.4f, 0.8f, 0.4f);
            
            if (GUILayout.Button("Apply Changes", EditorStyles.miniButton, GUILayout.Width(200), GUILayout.Height(30)))
            {
                _onApply?.Invoke(_target);
                Close();
            }
            
            GUI.color = oldColor;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
        }

        private static object Clone(object source)
        {
            if (source is ICloneable cloneable) return cloneable.Clone();
            return source;
        }
    }
}