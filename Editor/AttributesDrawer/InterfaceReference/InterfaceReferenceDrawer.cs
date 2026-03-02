using UniCore.Attribute;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.AttributesDrawer
{
    [CustomPropertyDrawer(typeof(InterfaceReferenceAttribute))]
    public class InterfaceReferenceAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InterfaceReferenceUtility.OnGUI(position, property, label, fieldInfo.GetArguments((InterfaceReferenceAttribute)attribute));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return InterfaceReferenceUtility.GetPropertyHeight(property, fieldInfo.GetArguments((InterfaceReferenceAttribute)attribute));
        }
    }

    [CustomPropertyDrawer(typeof(InterfaceReference<>))]
    [CustomPropertyDrawer(typeof(InterfaceReference<,>))]
    public class InterfaceReferenceDrawer : PropertyDrawer
    {
        private const string fieldName = "_underlyingValue";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prop = property.FindPropertyRelative(fieldName);
            InterfaceReferenceUtility.OnGUI(position, prop, label, fieldInfo.GetArguments());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var prop = property.FindPropertyRelative(fieldName);
            return InterfaceReferenceUtility.GetPropertyHeight(prop, fieldInfo.GetArguments());
        }
    }
}