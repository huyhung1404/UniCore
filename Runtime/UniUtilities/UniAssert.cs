using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniUtilities
{
    public static class UniAssert
    {
        private const string k_editorSymbol = "UNITY_EDITOR";
        private const string k_debugSymbol = "DEVELOPMENT_BUILD";
        private const string k_tagError = "<color=#FF4444><b>[UniAssert]</b></color>";
        
        [Conditional(k_editorSymbol), Conditional(k_debugSymbol)]
        public static void IsNotNull(object obj, string memberName, Object context = null)
        {
            if (obj == null || obj.Equals(null))
            {
                LogFailure($"Reference '{memberName}' is null.", context);
            }
        }
        
        [Conditional(k_editorSymbol), Conditional(k_debugSymbol)]
        public static void IsNotEmpty(IEnumerable collection, string memberName, Object context = null)
        {
            if (collection == null)
            {
                LogFailure($"Collection '{memberName}' is null.", context);
                return;
            }

            var enumerator = collection.GetEnumerator();
            using var disposable = enumerator as IDisposable;
            if (!enumerator.MoveNext())
            {
                LogFailure($"Collection '{memberName}' is empty.", context);
            }
        }
        
        [Conditional(k_editorSymbol), Conditional(k_debugSymbol)]
        public static void IsInRange(float value, float min, float max, string memberName, Object context = null)
        {
            if (value < min || value > max)
            {
                LogFailure($"Value '{memberName}' ({value}) is out of range [{min}, {max}].", context);
            }
        }
        
        [Conditional(k_editorSymbol), Conditional(k_debugSymbol)]
        public static void HasLayer(GameObject go, string layerName, Object context = null)
        {
            if (go == null) return;

            int layerIndex = LayerMask.NameToLayer(layerName);
            if (go.layer != layerIndex)
            {
                LogFailure($"GameObject '{go.name}' must be on layer '{layerName}'. Current layer: {LayerMask.LayerToName(go.layer)}.", context);
            }
        }
        
        [Conditional(k_editorSymbol)]
        public static void IsPrefab(GameObject go, string memberName, Object context = null)
        {
#if UNITY_EDITOR
            if (go != null && !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go))
            {
                LogFailure($"'{memberName}' must be a Prefab Asset from Project folders.", context);
            }
#endif
        }
        
        [Conditional(k_editorSymbol), Conditional(k_debugSymbol)]
        public static void IsTrue(bool condition, string message, Object context = null)
        {
            if (!condition)
            {
                LogFailure($"Assertion failed: {message}", context);
            }
        }

        private static void LogFailure(string message, Object context)
        {
            var contextName = context != null ? $"[{context.name}] " : "";
            UnityEngine.Debug.LogError($"{k_tagError} {contextName}{message}", context);
        }
    }
}