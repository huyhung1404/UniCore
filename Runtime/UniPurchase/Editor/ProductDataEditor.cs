#if ENABLE_UNI_PURCHASE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPurchase.Editor
{
    [CustomEditor(typeof(ProductData), true)]
    public class ProductDataEditor : UnityEditor.Editor
    {
        private static readonly HashSet<string> s_basePropertyNames = new HashSet<string>
        {
            "m_Script", "m_ObjectHideFlags", "m_Name",
            "m_productId", "m_productType", "m_discountPercent", "m_price"
        };

        private bool _derivedFoldout;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var idProp = serializedObject.FindProperty("m_productId");
            var typeProp = serializedObject.FindProperty("m_productType");
            var priceProp = serializedObject.FindProperty("m_price");
            var discountProp = serializedObject.FindProperty("m_discountPercent");

            var line1 = EditorGUILayout.GetControlRect();
            var halfWidth = line1.width / 2f;
            const float padding = 5f;

            var idRect = new Rect(line1.x, line1.y, halfWidth - padding, line1.height);
            var typeRect = new Rect(line1.x + halfWidth, line1.y, halfWidth, line1.height);

            EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
            if (string.IsNullOrEmpty(idProp.stringValue))
            {
                EditorGUI.LabelField(new Rect(idRect.x + 4, idRect.y, idRect.width, idRect.height),
                    "Enter Product ID...", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

            var line2 = EditorGUILayout.GetControlRect();
            var priceRect = new Rect(line2.x, line2.y, halfWidth - padding, line2.height);
            var discountRect = new Rect(line2.x + halfWidth, line2.y, halfWidth, line2.height);

            var originalLabelWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 45;
            EditorGUI.PropertyField(priceRect, priceProp, new GUIContent("Price", "Simulated Price for Editor UI"));

            EditorGUIUtility.labelWidth = 65;
            EditorGUI.PropertyField(discountRect, discountProp, new GUIContent("Discount", "0.0 = No discount, 0.5 = 50% off"));

            EditorGUIUtility.labelWidth = originalLabelWidth;

            if (target.GetType() != typeof(ProductData))
            {
                EditorGUILayout.Space(4);
                _derivedFoldout = EditorGUILayout.Foldout(_derivedFoldout, $"Extended Data ({target.GetType().Name})", true);
                if (_derivedFoldout)
                {
                    EditorGUI.indentLevel++;
                    var iterator = serializedObject.GetIterator();
                    iterator.NextVisible(true);
                    while (iterator.NextVisible(false))
                    {
                        if (s_basePropertyNames.Contains(iterator.name)) continue;
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
