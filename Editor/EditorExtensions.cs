using System;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor
{
    public static class EditorExtensions
    {
        public static (bool, object) DrawDefaultValue(Type type, GUIContent label, object value)
        {
            if (type == typeof(int)) return (true, EditorGUILayout.IntField(label, (int)(value ?? 0)));
            if (type == typeof(float)) return (true, EditorGUILayout.FloatField(label, (float)(value ?? 0f)));
            if (type == typeof(bool)) return (true, EditorGUILayout.Toggle(label, (bool)(value ?? false)));
            if (type == typeof(string)) return (true, EditorGUILayout.TextField(label, (string)value ?? string.Empty));
            if (type == typeof(double)) return (true, EditorGUILayout.DoubleField(label, (double)(value ?? 0.0)));
            if (type == typeof(long)) return (true, EditorGUILayout.LongField(label, (long)(value ?? 0L)));
            
            if (type == typeof(Vector2)) return (true, EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero));
            if (type == typeof(Vector3)) return (true, EditorGUILayout.Vector3Field(label, value != null ? (Vector3)value : Vector3.zero));
            if (type == typeof(Vector4)) return (true, EditorGUILayout.Vector4Field(label, value != null ? (Vector4)value : Vector4.zero));
            if (type == typeof(Color)) return (true, EditorGUILayout.ColorField(label, value != null ? (Color)value : Color.white));
            if (type == typeof(AnimationCurve)) return (true, EditorGUILayout.CurveField(label, value as AnimationCurve ?? new AnimationCurve()));
            if (type == typeof(Bounds)) return (true, EditorGUILayout.BoundsField(label, value != null ? (Bounds)value : new Bounds()));
            if (type == typeof(Rect)) return (true, EditorGUILayout.RectField(label, value != null ? (Rect)value : new Rect()));
            
            if (type.IsEnum)
            {
                var enumValue = (Enum)(value ?? Activator.CreateInstance(type));
                return (true, EditorGUILayout.EnumPopup(label, enumValue));
            }
            
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var objValue = value as UnityEngine.Object;
                var newObj = EditorGUILayout.ObjectField(label, objValue, type, true);
                return (true, newObj);
            }

            return (false, null);
        }
    }
}