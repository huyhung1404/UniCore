using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniCore.Attribute
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InterfaceReferenceAttribute : PropertyAttribute
    {
        public Type ReferenceType { get; private set; }

        public InterfaceReferenceAttribute(Type type)
        {
            ReferenceType = type;
        }
    }

    [Serializable]
    public class InterfaceReference<TInterface, UObject> where UObject : Object where TInterface : class
    {
        [SerializeField] [HideInInspector] private UObject _underlyingValue;

        public TInterface Value
        {
            get
            {
                if (_underlyingValue == null) return null;
                var @interface = _underlyingValue as TInterface;
                Debug.Assert(@interface != null, $"{_underlyingValue} needs to implement interface {nameof(TInterface)}.");
                return @interface;
            }
            set
            {
                if (value == null)
                    _underlyingValue = null;
                else
                {
                    var newValue = value as UObject;
                    Debug.Assert(newValue != null, $"{value} needs to be of type {typeof(UObject)}.");
                    _underlyingValue = newValue;
                }
            }
        }

        public UObject UnderlyingValue { get => _underlyingValue; set => _underlyingValue = value; }

        public InterfaceReference()
        {
        }

        public InterfaceReference(UObject target) => _underlyingValue = target;
        public InterfaceReference(TInterface @interface) => _underlyingValue = @interface as UObject;

        public static implicit operator TInterface(InterfaceReference<TInterface, UObject> obj) => obj.Value;
    }

    [Serializable]
    public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class
    {
        public static implicit operator TInterface(InterfaceReference<TInterface> obj) => obj.Value;
    }
}