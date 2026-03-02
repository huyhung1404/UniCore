using UnityEngine;

namespace UniCore.Editor.Windows
{
    public static class SignalDebugUtil
    {
        public static string GetSource(object listener)
        {
            if (listener is not MonoBehaviour mb) return "Non-Mono";
            var scene = mb.gameObject.scene;
            return scene.IsValid() ? $"{scene.name} / {mb.gameObject.name}" : "No Scene";
        }

        public static bool TryGetUnityObject(object listener, out Object obj)
        {
            if (listener is MonoBehaviour mb)
            {
                obj = mb.gameObject;
                return true;
            }

            obj = null;
            return false;
        }
    }
}