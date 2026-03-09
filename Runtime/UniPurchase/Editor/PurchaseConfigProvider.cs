using UnityEditor;
using UnityEngine;
#if ENABLE_UNI_PURCHASE
using UnityEditorInternal;
#endif

namespace UniPurchase.Editor
{
    public static class PurchaseConfigProvider
    {
#if ENABLE_UNI_PURCHASE
        private static ReorderableList s_productList;
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

            if (s_productList == null || s_productList.serializedProperty.serializedObject != serializedObject)
            {
                InitializeReorderableList(serializedObject, productsProp);
            }

            s_productList?.DoLayoutList();
        }

        private static void InitializeReorderableList(SerializedObject serializedObject, SerializedProperty productsProp)
        {
            s_productList = new ReorderableList(serializedObject, productsProp, true, true, true, true);

            s_productList.drawHeaderCallback = rect => { EditorGUI.LabelField(new Rect(rect.x + 15, rect.y, rect.width, rect.height), "Product List"); };

            s_productList.elementHeightCallback = _ => (EditorGUIUtility.singleLineHeight * 2) + 8f;

            s_productList.drawElementBackgroundCallback = (rect, index, active, focused) =>
            {
                if (active)
                {
                    var activeColor = focused ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    EditorGUI.DrawRect(rect, activeColor);
                }
                else if (index % 2 == 0)
                {
                    EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.05f));
                }
            };

            s_productList.drawElementCallback = (rect, index, _, _) =>
            {
                var element = s_productList.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 3;

                var idProp = element.FindPropertyRelative("m_productId");
                var typeProp = element.FindPropertyRelative("m_productType");
                var priceProp = element.FindPropertyRelative("m_price");
                var discountProp = element.FindPropertyRelative("m_discountPercent");

                var halfWidth = rect.width / 2f;
                var padding = 5f;

                var line1Rect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                var idRect = new Rect(line1Rect.x, line1Rect.y, halfWidth - padding, line1Rect.height);
                var typeRect = new Rect(line1Rect.x + halfWidth, line1Rect.y, halfWidth, line1Rect.height);

                EditorGUI.PropertyField(idRect, idProp, GUIContent.none);
                if (string.IsNullOrEmpty(idProp.stringValue))
                {
                    EditorGUI.LabelField(new Rect(idRect.x + 4, idRect.y, idRect.width, idRect.height), "Enter Product ID...", EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

                var line2Rect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight + 2, rect.width, EditorGUIUtility.singleLineHeight);
                var priceRect = new Rect(line2Rect.x, line2Rect.y, halfWidth - padding, line2Rect.height);
                var discountRect = new Rect(line2Rect.x + halfWidth, line2Rect.y, halfWidth, line2Rect.height);

                var originalLabelWidth = EditorGUIUtility.labelWidth;

                EditorGUIUtility.labelWidth = 45;
                EditorGUI.PropertyField(priceRect, priceProp, new GUIContent("Price", "Simulated Price for Editor UI"));

                EditorGUIUtility.labelWidth = 65;
                EditorGUI.PropertyField(discountRect, discountProp, new GUIContent("Discount", "0.0 = No discount, 0.5 = 50% off"));

                EditorGUIUtility.labelWidth = originalLabelWidth;
            };
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