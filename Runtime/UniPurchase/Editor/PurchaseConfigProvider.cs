using UnityEditor;
using UnityEngine;
#if ENABLE_UNI_PURCHASE
using System.Collections.Generic;
using UnityEditorInternal;
#endif

namespace UniPurchase.Editor
{
    public static class PurchaseConfigProvider
    {
#if ENABLE_UNI_PURCHASE
        private static ReorderableList s_productList;
        private static readonly Dictionary<int, bool> s_foldoutStates = new Dictionary<int, bool>();
        private static Dictionary<int, string> s_validationErrors = new Dictionary<int, string>();

        private static readonly HashSet<string> s_basePropertyNames = new HashSet<string>
        {
            "m_Script", "m_ObjectHideFlags", "m_Name",
            "m_productId", "m_productType", "m_discountPercent", "m_price"
        };

        private const float k_Padding = 3f;
        private const float k_LineSpacing = 2f;
        private const float k_ErrorBoxHeight = 26f;
        private static bool s_headerDragHighlight;
#endif

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/UniCore/Purchasing", SettingsScope.Project)
            {
                keywords = new[] { "Purchase", "UniCore", "Purchasing" },
                guiHandler = (_) =>
                {
#if ENABLE_UNI_PURCHASE
                    var config = EditorPurchaseConfig.instance;
                    if (config == null) return;

                    var serializedObject = new SerializedObject(config);
                    serializedObject.Update();
#endif
                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(5, 5, 5, 5) });
                    EditorGUI.BeginChangeCheck();

#if ENABLE_UNI_PURCHASE
                    OnGUI(serializedObject);
#else
                    DrawMissingDependenciesWarning();
#endif
#if ENABLE_UNI_PURCHASE
                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        config.SaveData();
                    }
#endif
                    EditorGUILayout.Space(20);
                    EditorGUILayout.EndVertical();
                }
            };
            return provider;
        }

#if ENABLE_UNI_PURCHASE
        private static void OnGUI(SerializedObject serializedObject)
        {
            var enableProp = serializedObject.FindProperty("m_isEnabled");
            var isSystemEnabled = DrawMasterToggle(enableProp);
            if (!isSystemEnabled) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                var icon = EditorGUIUtility.IconContent("d_FilterByLabel") ?? EditorGUIUtility.IconContent("SettingsIcon");

                if (icon != null && icon.image != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                }

                GUILayout.Label("In-App Products Configuration", new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleLeft });
            }

            EditorGUILayout.Space(5);

            var productsProp = serializedObject.FindProperty("m_products");

            s_validationErrors = ValidateProducts(productsProp);

            if (s_productList == null || s_productList.serializedProperty.serializedObject != serializedObject)
            {
                InitializeReorderableList(serializedObject, productsProp);
            }

            s_productList?.DoLayoutList();
        }

        private static Dictionary<int, string> ValidateProducts(SerializedProperty productsProp)
        {
            var errors = new Dictionary<int, string>();
            var seenObjects = new Dictionary<int, int>();
            var seenIds = new Dictionary<string, int>();

            for (int i = 0; i < productsProp.arraySize; i++)
            {
                var element = productsProp.GetArrayElementAtIndex(i);
                var productData = element.objectReferenceValue as ProductData;
                if (productData == null) continue;

                int instanceId = productData.GetInstanceID();
                if (seenObjects.TryGetValue(instanceId, out var firstRefIdx))
                {
                    errors[i] = $"Duplicate reference! Same as element [{firstRefIdx}]";
                    continue;
                }

                seenObjects[instanceId] = i;

                string productId = productData.ProductId;
                if (string.IsNullOrEmpty(productId)) continue;

                if (seenIds.TryGetValue(productId, out var firstIdIdx))
                {
                    errors[i] = $"Duplicate Product ID '{productId}'! Same as element [{firstIdIdx}]";
                    if (!errors.ContainsKey(firstIdIdx))
                        errors[firstIdIdx] = $"Duplicate Product ID '{productId}'!";
                }
                else
                {
                    seenIds[productId] = i;
                }
            }

            return errors;
        }

        private static void InitializeReorderableList(SerializedObject serializedObject, SerializedProperty productsProp)
        {
            s_productList = new ReorderableList(serializedObject, productsProp, true, true, true, true);

            s_productList.drawHeaderCallback = rect =>
            {
                HandleHeaderDragAndDrop(rect);
                EditorGUI.LabelField(new Rect(rect.x + 15, rect.y, rect.width, rect.height), "Product List");
            };

            s_productList.elementHeightCallback = index =>
            {
                var lineHeight = EditorGUIUtility.singleLineHeight;
                float height = k_Padding * 2 + lineHeight + k_LineSpacing;

                if (index >= s_productList.serializedProperty.arraySize) return height;

                var element = s_productList.serializedProperty.GetArrayElementAtIndex(index);
                var productData = element.objectReferenceValue as ProductData;
                if (productData == null) return height;

                height += (lineHeight + k_LineSpacing) * 2;

                if (s_validationErrors.ContainsKey(index))
                    height += k_ErrorBoxHeight + k_LineSpacing;

                if (productData.GetType() != typeof(ProductData))
                {
                    height += lineHeight + k_LineSpacing;
                    int instanceId = productData.GetInstanceID();
                    if (s_foldoutStates.TryGetValue(instanceId, out var isOpen) && isOpen)
                        height += GetDerivedPropertiesHeight(productData);
                }

                return height;
            };

            s_productList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                bool hasError = s_validationErrors.ContainsKey(index);

                if (hasError)
                {
                    EditorGUI.DrawRect(rect, new Color(0.8f, 0.15f, 0.15f, 0.18f));
                }

                if (active)
                {
                    var activeColor = focused ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    EditorGUI.DrawRect(rect, activeColor);
                }
                else if (!hasError && index % 2 == 0)
                {
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.05f));
                }
            };

            s_productList.drawElementCallback = (rect, index, _, _) =>
            {
                var element = s_productList.serializedProperty.GetArrayElementAtIndex(index);
                var productData = element.objectReferenceValue as ProductData;

                float y = rect.y + k_Padding;
                var lineHeight = EditorGUIUtility.singleLineHeight;

                const float removeButtonWidth = 20f;
                var objFieldRect = new Rect(rect.x, y, rect.width - removeButtonWidth - 2f, lineHeight);
                EditorGUI.PropertyField(objFieldRect, element, GUIContent.none);

                var removeBtnRect = new Rect(rect.x + rect.width - removeButtonWidth, y, removeButtonWidth, lineHeight);
                if (GUI.Button(removeBtnRect, "−", EditorStyles.miniButton))
                {
                    RemoveProductAtIndex(index);
                    return;
                }

                y += lineHeight + k_LineSpacing;

                if (productData == null) return;

                var so = new SerializedObject(productData);
                so.Update();

                var idProp = so.FindProperty("m_productId");
                var typeProp = so.FindProperty("m_productType");
                var priceProp = so.FindProperty("m_price");
                var discountProp = so.FindProperty("m_discountPercent");

                var halfWidth = rect.width / 2f;
                var fieldPadding = 5f;

                var idRect = new Rect(rect.x, y, halfWidth - fieldPadding, lineHeight);
                var typeRect = new Rect(rect.x + halfWidth, y, halfWidth, lineHeight);

                EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
                if (string.IsNullOrEmpty(idProp.stringValue))
                {
                    EditorGUI.LabelField(new Rect(idRect.x + 4, idRect.y, idRect.width, idRect.height), "Enter Product ID...", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
                y += lineHeight + k_LineSpacing;

                var priceRect = new Rect(rect.x, y, halfWidth - fieldPadding, lineHeight);
                var discountRect = new Rect(rect.x + halfWidth, y, halfWidth, lineHeight);

                var originalLabelWidth = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 45;
                EditorGUI.PropertyField(priceRect, priceProp, new GUIContent("Price", "Simulated Price for Editor UI"));

                EditorGUIUtility.labelWidth = 65;
                EditorGUI.PropertyField(discountRect, discountProp, new GUIContent("Discount", "0.0 = No discount, 0.5 = 50% off"));

                EditorGUIUtility.labelWidth = originalLabelWidth;
                y += lineHeight + k_LineSpacing;

                if (s_validationErrors.TryGetValue(index, out var error))
                {
                    var errorRect = new Rect(rect.x, y, rect.width, k_ErrorBoxHeight);
                    EditorGUI.HelpBox(errorRect, error, MessageType.Error);
                    y += k_ErrorBoxHeight + k_LineSpacing;
                }

                if (productData.GetType() != typeof(ProductData))
                {
                    DrawDerivedPropertiesFoldout(rect, productData, so, y);
                }

                if (so.ApplyModifiedProperties())
                {
                    GUI.changed = true;
                }
            };

            s_productList.onAddCallback = list =>
            {
                var prop = list.serializedProperty;
                prop.arraySize++;
                prop.serializedObject.ApplyModifiedProperties();
                list.index = prop.arraySize - 1;
            };

            s_productList.onRemoveCallback = list =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(list.index);
                if (element.objectReferenceValue != null)
                    element.objectReferenceValue = null;
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                list.serializedProperty.serializedObject.ApplyModifiedProperties();
            };
        }

        private static void HandleHeaderDragAndDrop(Rect rect)
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragUpdated when rect.Contains(evt.mousePosition):
                {
                    if (HasValidProductDrag())
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        s_headerDragHighlight = true;
                        evt.Use();
                    }

                    break;
                }
                case EventType.DragPerform when rect.Contains(evt.mousePosition):
                {
                    DragAndDrop.AcceptDrag();
                    var prop = s_productList.serializedProperty;
                    var so = prop.serializedObject;

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (!(obj is ProductData product)) continue;
                        if (IsProductInList(prop, product)) continue;

                        prop.arraySize++;
                        so.ApplyModifiedProperties();
                        prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = product;
                        so.ApplyModifiedProperties();
                    }

                    s_headerDragHighlight = false;
                    evt.Use();
                    break;
                }
                case EventType.DragExited:
                    s_headerDragHighlight = false;
                    break;
                case EventType.Repaint when s_headerDragHighlight:
                    EditorGUI.DrawRect(rect, new Color(0.3f, 0.7f, 1f, 0.2f));
                    break;
            }
        }

        private static bool HasValidProductDrag()
        {
            if (DragAndDrop.objectReferences == null) return false;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is ProductData) return true;
            }

            return false;
        }

        private static void RemoveProductAtIndex(int index)
        {
            var prop = s_productList.serializedProperty;
            var element = prop.GetArrayElementAtIndex(index);
            if (element.objectReferenceValue != null)
                element.objectReferenceValue = null;
            prop.DeleteArrayElementAtIndex(index);
            prop.serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }

        private static bool IsProductInList(SerializedProperty listProp, Object product)
        {
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == product)
                    return true;
            }

            return false;
        }

        private static void DrawDerivedPropertiesFoldout(Rect rect, ProductData productData, SerializedObject so, float startY)
        {
            int instanceId = productData.GetInstanceID();
            s_foldoutStates.TryGetValue(instanceId, out var isOpen);

            var foldoutRect = new Rect(rect.x, startY, rect.width, EditorGUIUtility.singleLineHeight);
            var newOpen = EditorGUI.Foldout(foldoutRect, isOpen, $"Extended Data ({productData.GetType().Name})", true);
            s_foldoutStates[instanceId] = newOpen;

            if (!newOpen) return;

            float y = startY + EditorGUIUtility.singleLineHeight + k_LineSpacing;
            var iterator = so.GetIterator();
            iterator.NextVisible(true);
            while (iterator.NextVisible(false))
            {
                if (s_basePropertyNames.Contains(iterator.name)) continue;

                var propertyHeight = EditorGUI.GetPropertyHeight(iterator, true);
                var propRect = new Rect(rect.x + 15f, y, rect.width - 15f, propertyHeight);
                EditorGUI.PropertyField(propRect, iterator, true);
                y += propertyHeight + k_LineSpacing;
            }
        }

        private static float GetDerivedPropertiesHeight(ProductData productData)
        {
            var so = new SerializedObject(productData);
            float totalHeight = 0f;
            var iter = so.GetIterator();
            iter.NextVisible(true);
            while (iter.NextVisible(false))
            {
                if (!s_basePropertyNames.Contains(iter.name))
                    totalHeight += EditorGUI.GetPropertyHeight(iter, true) + k_LineSpacing;
            }

            return totalHeight;
        }

        public static bool DrawMasterToggle(SerializedProperty enableProp)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space(5);
                enableProp.boolValue = DrawSwitchToggle(enableProp.boolValue);
                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(15);
            return enableProp.boolValue;
        }

        public static bool DrawSwitchToggle(bool value)
        {
            var rect = GUILayoutUtility.GetRect(50, 24);
            var e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                value = !value;
                GUI.changed = true;
                e.Use();
            }

            if (e.type != EventType.Repaint) return value;
            var bgColor = value ? new Color(0.2f, 0.84f, 0.29f) : new Color(0.45f, 0.45f, 0.45f);

            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, bgColor, 0, rect.height / 2f);

            const float padding = 2f;
            var knobSize = rect.height - padding * 2f;

            var knobX = value ? (rect.x + rect.width - knobSize - padding) : (rect.x + padding);
            var knobRect = new Rect(knobX, rect.y + padding, knobSize, knobSize);

            var shadowRect = new Rect(knobRect.x, knobRect.y + 1.5f, knobSize, knobSize);
            GUI.DrawTexture(shadowRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0f, 0f, 0f, 0.35f), 0, knobSize / 2f);
            GUI.DrawTexture(knobRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.white, 0, knobSize / 2f);

            return value;
        }
#else
        private static void DrawMissingDependenciesWarning()
        {
            var warningStyle = new GUIStyle("helpbox")
            {
                fontSize = 13,
                padding = new RectOffset(5, 5, 10, 0),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            EditorGUILayout.BeginVertical(warningStyle);

            GUILayout.Label(EditorGUIUtility.IconContent("console.erroricon"), new GUIStyle { alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.Space(5);

            GUILayout.Label("MISSING DEPENDENCIES!", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
            EditorGUILayout.Space(10);

            GUILayout.Label("The UniPurchase system requires the following packages to function:", EditorStyles.label);
            EditorGUILayout.Space(5);
            GUILayout.Label("• com.unity.purchasing (>= 5.0.0)", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();
        }
#endif
    }
}