using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UniCore.Attribute;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.AttributesDrawer
{
    public static class InterfaceReferenceUtility
    {
        private const float k_helpBoxHeight = 24;
        private static GUIStyle _normalInterfaceLabelStyle;
        private static bool _isOpeningQueued;
        private static Texture2D _styleTexture;

        public static void OnGUI(Rect position, SerializedProperty property, GUIContent label, InterfaceObjectArguments args)
        {
            InitializeStyleIfNeeded();

            var prevValue = property.objectReferenceValue;
            position.height = EditorGUIUtility.singleLineHeight;
            var prevColor = GUI.backgroundColor;
            if (IsAssignedAndHasWrongInterface(prevValue, args))
            {
                ShowWrongInterfaceErrorBox(position, prevValue, args);
                GUI.backgroundColor = Color.red;
            }

            var prevEnabledState = GUI.enabled;
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition) && GUI.enabled &&
                !CanAssign(DragAndDrop.objectReferences, args, true)) GUI.enabled = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.ObjectField(position, property, args.ObjectType, label);
            if (EditorGUI.EndChangeCheck())
            {
                var newVal = GetClosestAssignableComponent(property.objectReferenceValue, args);
                if (newVal != null && !CanAssign(newVal, args))
                    property.objectReferenceValue = prevValue;
                property.objectReferenceValue = newVal;
            }

            GUI.backgroundColor = prevColor;
            GUI.enabled = prevEnabledState;

            var controlID = GUIUtility.GetControlID(FocusType.Passive) - 1;
            var isHovering = position.Contains(Event.current.mousePosition);
            DrawInterfaceNameLabel(position, prevValue == null || isHovering ? $"({args.InterfaceType.Name})" : "*", controlID);
            ReplaceObjectPickerForControl(property, args, controlID);
        }

        private static void ShowWrongInterfaceErrorBox(Rect position, UnityEngine.Object prevValue, InterfaceObjectArguments args)
        {
            var helpBoxPosition = position;
            helpBoxPosition.y += position.height;
            helpBoxPosition.height = k_helpBoxHeight;
            EditorGUI.HelpBox(helpBoxPosition, $"Object {prevValue.name} needs to implement the required interface {args.InterfaceType}.", MessageType.Error);
        }

        private static void ReplaceObjectPickerForControl(SerializedProperty property, InterfaceObjectArguments args, int controlID)
        {
            var currentObjectPickerID = EditorGUIUtility.GetObjectPickerControlID();
            if (controlID != currentObjectPickerID || _isOpeningQueued) return;
            if (EditorWindow.focusedWindow == null) return;
            _isOpeningQueued = true;
            EditorApplication.delayCall += () => OpenDelayed(property, args);
        }

        private static void DrawInterfaceNameLabel(Rect position, string displayString, int controlID)
        {
            if (Event.current.type != EventType.Repaint) return;
            const int additionalLeftWidth = 3;
            const int verticalIndent = 1;
            var content = EditorGUIUtility.TrTextContent(displayString);
            var size = _normalInterfaceLabelStyle.CalcSize(content);
            var interfaceLabelPosition = position;
            interfaceLabelPosition.width = size.x + additionalLeftWidth;
            interfaceLabelPosition.x += position.width - interfaceLabelPosition.width - 18;
            interfaceLabelPosition.height -= verticalIndent * 2;
            interfaceLabelPosition.y += verticalIndent;
            _normalInterfaceLabelStyle.Draw(interfaceLabelPosition, EditorGUIUtility.TrTextContent(displayString), controlID, DragAndDrop.activeControlID == controlID, false);
        }

        private static void InitializeStyleIfNeeded()
        {
            if (_normalInterfaceLabelStyle != null) return;
            _normalInterfaceLabelStyle = new GUIStyle(EditorStyles.label);
            var objectFieldStyle = EditorStyles.objectField;
            _normalInterfaceLabelStyle.font = objectFieldStyle.font;
            _normalInterfaceLabelStyle.fontSize = objectFieldStyle.fontSize;
            _normalInterfaceLabelStyle.fontStyle = objectFieldStyle.fontStyle;
            _normalInterfaceLabelStyle.alignment = TextAnchor.MiddleRight;
            _normalInterfaceLabelStyle.padding = new RectOffset(0, 2, 0, 0);
            _styleTexture = new Texture2D(1, 1);
            _styleTexture.SetPixel(0, 0, new Color(40 / 255f, 40 / 255f, 40 / 255f));
            _styleTexture.Apply();
            _normalInterfaceLabelStyle.normal.background = _styleTexture;
        }

        public static float GetPropertyHeight(SerializedProperty property, InterfaceObjectArguments args)
        {
            if (IsAssignedAndHasWrongInterface(property.objectReferenceValue, args)) return EditorGUIUtility.singleLineHeight + k_helpBoxHeight;
            return EditorGUIUtility.singleLineHeight;
        }
        
        private static void OpenDelayed(SerializedProperty property, InterfaceObjectArguments args)
        {
            var win = EditorWindow.focusedWindow;
            win.Close();

            var derivedTypes = TypeCache.GetTypesDerivedFrom(args.InterfaceType);
            var sb = new StringBuilder();
            foreach (var type in derivedTypes.Where(type => args.ObjectType.IsAssignableFrom(type)))
            {
                sb.Append("t:" + type.FullName + " ");
            }

            if (sb.Length == 0) sb.Append("t:");

            var filter = new ObjectSelectorFilter(sb.ToString(), obj => CanAssign(obj, args));
            InterfaceSelectorWindow.Show(property, obj =>
            {
                property.objectReferenceValue = obj;
                property.serializedObject.ApplyModifiedProperties();
            }, (obj, success) =>
            {
                if (success) property.objectReferenceValue = obj;
            }, filter, args.ObjectType);
            InterfaceSelectorWindow.s_instance.position = win.position;
            var content = new GUIContent($"Select {args.ObjectType.Name} ({args.InterfaceType.Name})");
            InterfaceSelectorWindow.s_instance.titleContent = content;
            _isOpeningQueued = false;
        }

        private static UnityEngine.Object GetClosestAssignableComponent(UnityEngine.Object obj, InterfaceObjectArguments args)
        {
            if (CanAssign(obj, args))
                return obj;
            if (!args.ObjectType.IsSubclassOf(typeof(Component))) return null;
            if (obj is GameObject go && TryFindSuitableComponent(go, args, out var foundComponent))
                return foundComponent;
            if (obj is Component comp && TryFindSuitableComponent(comp.gameObject, args, out foundComponent))
                return foundComponent;

            return null;
        }

        private static bool TryFindSuitableComponent(GameObject go, InterfaceObjectArguments args, out Component component)
        {
            foreach (var comp in go.GetComponents(args.ObjectType))
            {
                if (CanAssign(comp, args))
                {
                    component = comp;
                    return true;
                }
            }

            component = null;
            return false;
        }

        private static bool IsAssignedAndHasWrongInterface(UnityEngine.Object obj, InterfaceObjectArguments args) => obj != null && !args.InterfaceType.IsInstanceOfType(obj);

        private static bool CanAssign(UnityEngine.Object[] objects, InterfaceObjectArguments args, bool lookIntoGameObject = false) =>
            objects.All(obj => CanAssign(obj, args, lookIntoGameObject));

        private static bool CanAssign(UnityEngine.Object obj, InterfaceObjectArguments args, bool lookIntoGameObject = false)
        {
            if (obj == null) return false;
            if (args.InterfaceType.IsInstanceOfType(obj) && args.ObjectType.IsInstanceOfType(obj)) return true;
            return lookIntoGameObject && CanAssign(GetClosestAssignableComponent(obj, args), args);
        }

        internal static InterfaceObjectArguments GetArguments(this FieldInfo fieldInfo, InterfaceReferenceAttribute interfaceReferenceAttribute)
        {
            GetObjectAndInterfaceType(fieldInfo.FieldType, out var objectType);
            return new InterfaceObjectArguments(objectType, interfaceReferenceAttribute.ReferenceType);
        }

        private static void GetObjectAndInterfaceType(Type fieldType, out Type objectType)
        {
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IList<>))
            {
                objectType = fieldType.GetGenericArguments()[0];
                return;
            }

            objectType = fieldType;
        }

        internal static InterfaceObjectArguments GetArguments(this FieldInfo fieldInfo)
        {
            GetObjectAndInterfaceType(fieldInfo.FieldType, out var objectType, out var interfaceType);
            return new InterfaceObjectArguments(objectType, interfaceType);
        }

        private static void GetObjectAndInterfaceType(Type fieldType, out Type objectType, out Type interfaceType)
        {
            if (TryGetTypesFromInterfaceReference(fieldType, out objectType, out interfaceType)) return;
            TryGetTypesFromList(fieldType, out objectType, out interfaceType);
        }

        private static bool TryGetTypesFromInterfaceReference(Type fieldType, out Type objectType, out Type interfaceType)
        {
            var fieldBaseType = fieldType;
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(InterfaceReference<>)) fieldBaseType = fieldType.BaseType;
            if (fieldBaseType is { IsGenericType: true } && fieldBaseType.GetGenericTypeDefinition() == typeof(InterfaceReference<,>))
            {
                var types = fieldBaseType.GetGenericArguments();
                interfaceType = types[0];
                objectType = types[1];
                return true;
            }

            objectType = null;
            interfaceType = null;
            return false;
        }

        private static void TryGetTypesFromList(Type fieldType, out Type objectType, out Type interfaceType)
        {
            var listType = fieldType.GetInterfaces().FirstOrDefault(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IList<>));

            TryGetTypesFromInterfaceReference(listType?.GetGenericArguments()[0], out objectType, out interfaceType);
        }
    }

    public struct InterfaceObjectArguments
    {
        public Type ObjectType { get; private set; }
        public Type InterfaceType { get; private set; }

        public InterfaceObjectArguments(Type objectType, Type interfaceType)
        {
            Debug.Assert(typeof(UnityEngine.Object).IsAssignableFrom(objectType), $"{nameof(objectType)} needs to be of Type {typeof(UnityEngine.Object)}.");
            Debug.Assert(interfaceType.IsInterface, $"{nameof(interfaceType)} needs to be an interface.");
            ObjectType = objectType;
            InterfaceType = interfaceType;
        }
    }
}