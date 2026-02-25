using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniUtilities
{
    /// <summary>
    /// Cung cấp các phương thức kiểm định (Assertion) để đảm bảo tính toàn vẹn của hệ thống.
    /// Các phương thức này sẽ được loại bỏ hoàn toàn trong bản Build Release để tối ưu hiệu năng.
    /// </summary>
    public static class UniAssert
    {
        private const string k_EditorSymbol = "UNITY_EDITOR";
        private const string k_DebugSymbol = "DEVELOPMENT_BUILD";
        private const string k_TagError = "<color=#FF4444><b>[UniAssert]</b></color>";

        /// <summary>
        /// Đảm bảo đối tượng tham chiếu không bị null.
        /// Thường dùng để kiểm tra các SerializedField trong Awake/Start.
        /// </summary>
        [Conditional(k_EditorSymbol), Conditional(k_DebugSymbol)]
        public static void IsNotNull(object obj, string memberName, Object context = null)
        {
            if (obj == null || obj.Equals(null))
            {
                LogFailure($"Reference '{memberName}' is null.", context);
            }
        }

        /// <summary>
        /// Kiểm tra một tập hợp (List, Array,...) không được phép rỗng.
        /// </summary>
        [Conditional(k_EditorSymbol), Conditional(k_DebugSymbol)]
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

        /// <summary>
        /// Xác định một giá trị số thực phải nằm trong khoảng [min, max].
        /// </summary>
        [Conditional(k_EditorSymbol), Conditional(k_DebugSymbol)]
        public static void IsInRange(float value, float min, float max, string memberName, Object context = null)
        {
            if (value < min || value > max)
            {
                LogFailure($"Value '{memberName}' ({value}) is out of range [{min}, {max}].", context);
            }
        }

        /// <summary>
        /// Đảm bảo GameObject mục tiêu thuộc về một Layer cụ thể.
        /// </summary>
        [Conditional(k_EditorSymbol), Conditional(k_DebugSymbol)]
        public static void HasLayer(GameObject go, string layerName, Object context = null)
        {
            if (go == null) return;

            int layerIndex = LayerMask.NameToLayer(layerName);
            if (go.layer != layerIndex)
            {
                LogFailure($"GameObject '{go.name}' must be on layer '{layerName}'. Current layer: {LayerMask.LayerToName(go.layer)}.", context);
            }
        }

        /// <summary>
        /// Kiểm tra một GameObject phải là Prefab Asset, không được là Instance trong Scene.
        /// </summary>
        [Conditional(k_EditorSymbol)]
        public static void IsPrefab(GameObject go, string memberName, Object context = null)
        {
#if UNITY_EDITOR
            if (go != null && !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go))
            {
                LogFailure($"'{memberName}' must be a Prefab Asset from Project folders.", context);
            }
#endif
        }

        /// <summary>
        /// Kiểm tra tính đúng đắn của một điều kiện logic.
        /// </summary>
        [Conditional(k_EditorSymbol), Conditional(k_DebugSymbol)]
        public static void IsTrue(bool condition, string message, Object context = null)
        {
            if (!condition)
            {
                LogFailure($"Assertion failed: {message}", context);
            }
        }

        private static void LogFailure(string message, Object context)
        {
            string contextName = context != null ? $"[{context.name}] " : "";
            UnityEngine.Debug.LogError($"{k_TagError} {contextName}{message}", context);
        }
    }
}