#if UNITY_EDITOR
using System;
using UniCore.Audio;
using UnityEditor;
using UnityEngine;

#if HAS_UNITASK && HAS_ADDRESSABLES
using UniCore.Audio.Node;
#endif

namespace UniCore.Editor.Audio
{
    public static class AudioSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            var provider = new SettingsProvider("Project/UniCore/Audio", SettingsScope.Project)
            {
                keywords = new[] { "Audio", "UniCore", "Node", "Tree", "Settings", "Configuration", "Ducking", "Enable", "Disable" },
                guiHandler = (_) =>
                {
#if HAS_UNITASK && HAS_ADDRESSABLES
                    var config = AudioEditorSettings.instance;
                    if (config == null) return;

                    var serializedObject = new SerializedObject(config);
                    serializedObject.Update();
#endif
                    EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(5, 5, 5, 5) });
                    EditorGUI.BeginChangeCheck();

#if HAS_UNITASK && HAS_ADDRESSABLES
                    if (config.RootNode == null)
                    {
                        config.GetType().GetField("m_rootNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(config, new DirectionNode().WithName("Root"));
                        EditorUtility.SetDirty(config);
                    }

                    OnGUI(serializedObject);
#else
                    DrawMissingDependenciesWarning();
#endif
#if HAS_UNITASK && HAS_ADDRESSABLES
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

#if HAS_UNITASK && HAS_ADDRESSABLES

        #region Nút Play Editor (C++ Native) 🎧

        private static void PlayEditorAudio(AudioClip clip)
        {
            if (clip == null) return;
            var assembly = typeof(AudioImporter).Assembly;
            var type = assembly.GetType("UnityEditor.AudioUtil");
            var method = type?.GetMethod("PlayClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            method?.Invoke(null, new object[] { clip, 0, false });
        }

        private static void StopEditorAudio()
        {
            var assembly = typeof(AudioImporter).Assembly;
            var type = assembly.GetType("UnityEditor.AudioUtil");
            var method = type?.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            method?.Invoke(null, null);
        }

        #endregion

        private static void OnGUI(SerializedObject serializedObject)
        {
            var enableProp = serializedObject.FindProperty("m_isSystemEnabled");
            var isSystemEnabled = UniEditorGUI.DrawMasterToggle(enableProp);
            if (!isSystemEnabled) return;
            DrawGeneralSettings(serializedObject);
            GUILayout.Space(15);

            DrawAudioConfigurations(serializedObject);
            GUILayout.Space(15);

            var rootNodeProp = serializedObject.FindProperty("m_rootNode");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.IconContent("Audio Mixer"), GUILayout.Width(24), GUILayout.Height(24));
            GUILayout.Label("Audio Hierarchy", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft }, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1)), new Color(0.5f, 0.5f, 0.5f, 0.4f));
            GUILayout.Space(5);

            DrawNodeTreeRecursive(rootNodeProp, null, -1, 0);
        }

        private static void DrawGeneralSettings(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.IconContent("SettingsIcon"), GUILayout.Width(24), GUILayout.Height(24));
            GUILayout.Label("General Settings", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft }, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1)), new Color(0.5f, 0.5f, 0.5f, 0.4f));
            GUILayout.Space(5);

            var boxStyle = new GUIStyle("helpbox") { padding = new RectOffset(10, 10, 10, 10) };
            EditorGUILayout.BeginVertical(boxStyle);

            var audioMixer = serializedObject.FindProperty("m_outputMixer");
            EditorGUILayout.PropertyField(audioMixer, new GUIContent("Output"));
            if (audioMixer.objectReferenceValue == null) EditorGUILayout.HelpBox("Output mixer is not set.", MessageType.Error);

            var soundPrefab = serializedObject.FindProperty("m_soundEmitterPrefab");
            EditorGUILayout.PropertyField(soundPrefab, new GUIContent("Sound Emitter Prefab"));
            if (soundPrefab.objectReferenceValue == null) EditorGUILayout.HelpBox("Sound emitter prefab is not set.", MessageType.Error);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_poolInitialSize"), new GUIContent("Pool Initial Size"));
            EditorGUILayout.EndVertical();
        }

        private static void DrawAudioConfigurations(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(EditorGUIUtility.IconContent("AudioMixerGroup Icon"), GUILayout.Width(24), GUILayout.Height(24));
            GUILayout.Label("Audio Configurations", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleLeft }, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1)), new Color(0.5f, 0.5f, 0.5f, 0.4f));
            GUILayout.Space(5);

            var configsProp = serializedObject.FindProperty("m_configurations");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Profiles Count: {configsProp.arraySize}", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(new GUIContent(" + Add Profile"), EditorStyles.miniButton, GUILayout.Width(100)))
            {
                configsProp.arraySize++;
                var newElem = configsProp.GetArrayElementAtIndex(configsProp.arraySize - 1);

                newElem.FindPropertyRelative("Id").stringValue = $"NewProfile_{configsProp.arraySize}";
                newElem.FindPropertyRelative("Volume").floatValue = 1f;
                newElem.FindPropertyRelative("VolumeVariance").floatValue = 0f;
                newElem.FindPropertyRelative("Pitch").floatValue = 1f;
                newElem.FindPropertyRelative("PitchVariance").floatValue = 0f;
                newElem.FindPropertyRelative("SpatialBlend").floatValue = 0f;
                newElem.FindPropertyRelative("ReverbZoneMix").floatValue = 1f;
                newElem.FindPropertyRelative("DopplerLevel").floatValue = 1f;
                newElem.FindPropertyRelative("MaxDistance").floatValue = 50f;
                newElem.FindPropertyRelative("VolumeRolloff").enumValueIndex = (int)AudioRolloffMode.Logarithmic;

                newElem.FindPropertyRelative("IsDucking").boolValue = false;
                newElem.FindPropertyRelative("DuckingRatio").floatValue = 0.2f;
                newElem.FindPropertyRelative("DuckingFadeTime").floatValue = 0.5f;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            for (var i = 0; i < configsProp.arraySize; i++)
            {
                var configProp = configsProp.GetArrayElementAtIndex(i);
                var idProp = configProp.FindPropertyRelative("Id");

                var boxStyle = new GUIStyle("helpbox") { padding = new RectOffset(5, 5, 5, 5) };
                EditorGUILayout.BeginVertical(boxStyle);

                EditorGUILayout.BeginHorizontal();
                var foldoutRect = GUILayoutUtility.GetRect(15, 20, GUILayout.ExpandWidth(false));
                configProp.isExpanded = EditorGUI.Foldout(foldoutRect, configProp.isExpanded, GUIContent.none, true);

                GUILayout.Label(EditorGUIUtility.IconContent("AudioMixerController Icon"), GUILayout.Width(20), GUILayout.Height(20));
                idProp.stringValue = EditorGUILayout.TextField(idProp.stringValue, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
                {
                    configsProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (configProp.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(5);

                    var outputProp = configProp.FindPropertyRelative("Output");
                    EditorGUILayout.PropertyField(outputProp, new GUIContent("Output Mixer"));
                    EditorGUILayout.Space(5);

                    DrawConfigSection("Basic Settings", () =>
                    {
                        EditorGUILayout.Slider(configProp.FindPropertyRelative("Volume"), 0f, 1f, new GUIContent("Volume"));
                        EditorGUILayout.Slider(configProp.FindPropertyRelative("VolumeVariance"), 0f, 0.5f,
                            new GUIContent("Volume Var (\u00B1)", "Biến thiên âm lượng ngẫu nhiên"));

                        EditorGUILayout.Slider(configProp.FindPropertyRelative("Pitch"), -3f, 3f, new GUIContent("Pitch"));
                        EditorGUILayout.Slider(configProp.FindPropertyRelative("PitchVariance"), 0f, 1f, new GUIContent("Pitch Var (\u00B1)", "Biến thiên cao độ ngẫu nhiên"));

                        DrawSliderWithLabels(configProp.FindPropertyRelative("StereoPan"), -1f, 1f, new GUIContent("Stereo Pan"), "Left", "Right");
                        DrawSliderWithLabels(configProp.FindPropertyRelative("SpatialBlend"), 0f, 1f, new GUIContent("Spatial Blend"), "2D", "3D");

                        EditorGUILayout.Slider(configProp.FindPropertyRelative("ReverbZoneMix"), 0f, 1.1f, new GUIContent("Reverb Zone Mix"));
                        EditorGUILayout.IntSlider(configProp.FindPropertyRelative("Priority"), 0, 256, new GUIContent("Priority (0 = High)"));
                    });

                    DrawConfigSection("3D Settings", () =>
                    {
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("VolumeRolloff"), new GUIContent("Rolloff Mode"));
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("MinDistance"), new GUIContent("Min Distance"));
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("MaxDistance"), new GUIContent("Max Distance"));
                        EditorGUILayout.Slider(configProp.FindPropertyRelative("DopplerLevel"), 0f, 5f, new GUIContent("Doppler Level"));
                        EditorGUILayout.IntSlider(configProp.FindPropertyRelative("Spread"), 0, 360, new GUIContent("Spread"));
                    });

                    DrawConfigSection("Ducking System", () =>
                    {
                        var isDuckingProp = configProp.FindPropertyRelative("IsDucking");
                        EditorGUILayout.PropertyField(isDuckingProp, new GUIContent("Enable Auto-Ducking"));

                        if (isDuckingProp.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            EditorGUILayout.Space(2);
                            EditorGUILayout.Slider(configProp.FindPropertyRelative("DuckingRatio"), 0f, 1f, new GUIContent("Target Volume Ratio"));
                            EditorGUILayout.PropertyField(configProp.FindPropertyRelative("DuckingFadeTime"), new GUIContent("Fade Time"));
                            EditorGUI.indentLevel--;
                        }
                    });

                    DrawConfigSection("Effects & Bypass", () =>
                    {
                        var oldIndent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("Mute"), new GUIContent("Mute"), GUILayout.Width(50));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("BypassEffects"), new GUIContent("Bypass FX"), GUILayout.Width(80));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("BypassListenerEffects"), new GUIContent("Bypass Listener"), GUILayout.Width(110));
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(configProp.FindPropertyRelative("BypassReverbZones"), new GUIContent("Bypass Reverb"), GUILayout.Width(110));
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel = oldIndent;
                    });

                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(5);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(3);
            }
        }

        private static void DrawSliderWithLabels(SerializedProperty prop, float min, float max, GUIContent label, string leftText, string rightText)
        {
            EditorGUILayout.Slider(prop, min, max, label);

            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel, GUILayout.Height(12));

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var startX = rect.x + EditorGUIUtility.labelWidth - (oldIndent * 15f);
            var sliderWidth = rect.width - EditorGUIUtility.labelWidth - EditorGUIUtility.fieldWidth + (oldIndent * 15f) - 5f;

            if (sliderWidth > 0)
            {
                var leftStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperLeft, fontSize = 9 };
                var rightStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.UpperRight, fontSize = 9 };

                leftStyle.normal.textColor = rightStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1f);

                GUI.Label(new Rect(startX, rect.y - 2, sliderWidth / 2, rect.height), leftText, leftStyle);
                GUI.Label(new Rect(startX + sliderWidth / 2, rect.y - 2, sliderWidth / 2, rect.height), rightText, rightStyle);
            }

            EditorGUI.indentLevel = oldIndent;
            GUILayout.Space(3);
        }

        private static void DrawConfigSection(string title, Action drawContent)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical(new GUIStyle("helpbox"));

            EditorGUILayout.Space(5);
            drawContent?.Invoke();
            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private static void DrawNodeTreeRecursive(SerializedProperty nodeProp, SerializedProperty parentListProp, int indexInParent, int depth)
        {
            if (nodeProp == null) return;

            var typeName = nodeProp.managedReferenceFullTypename;
            var isDirection = typeName.Contains(nameof(DirectionNode));

            var nodeInstance = nodeProp.managedReferenceValue as BaseAudioNode;
            var logState = nodeInstance?.IsValid() ?? (Log.None, null);

            var bgRect = EditorGUILayout.BeginHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                var bgColor = (indexInParent % 2 == 0) ? new Color(0, 0, 0, 0.05f) : new Color(0, 0, 0, 0.1f);
                EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, EditorGUIUtility.currentViewWidth, bgRect.height), bgColor);
            }

            GUILayout.Space(depth * 15);
            if (depth > 0)
            {
                var lineRect = new Rect(bgRect.x + (depth * 15) - 7, bgRect.y, 1, bgRect.height);
                EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            }

            var foldoutRect = GUILayoutUtility.GetRect(15, 20, GUILayout.ExpandWidth(false));
            nodeProp.isExpanded = EditorGUI.Foldout(foldoutRect, nodeProp.isExpanded, GUIContent.none, true);

            var iconName = isDirection ? "Folder Icon" : "AudioClip Icon";
            GUILayout.Label(EditorGUIUtility.IconContent(iconName), GUILayout.Width(20), GUILayout.Height(20));

            var nameProp = nodeProp.FindPropertyRelative("m_nodeName");
            if (nameProp != null)
            {
                nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, EditorStyles.textField, GUILayout.ExpandWidth(true));
            }

            if (logState.Item1 == Log.Error)
            {
                GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml").image, logState.Item2), GUILayout.Width(20), GUILayout.Height(20));
            }
            else if (logState.Item1 == Log.Warning)
            {
                GUILayout.Label(new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml").image, logState.Item2), GUILayout.Width(20), GUILayout.Height(20));
            }

            GUILayout.Label($"[{GetShortTypeName(typeName)}]", EditorStyles.miniLabel, GUILayout.Width(160));

            if (isDirection)
            {
                if (GUILayout.Button(new GUIContent("+", "Add Audio Node"), EditorStyles.miniButtonRight, GUILayout.Width(25)))
                {
                    ShowAddContextMenu(nodeProp);
                }
            }
            else
            {
                GUILayout.Space(25);
            }

            if (parentListProp != null)
            {
                if (GUILayout.Button(new GUIContent("-", "Delete Node"), EditorStyles.miniButtonRight, GUILayout.Width(25)))
                {
                    parentListProp.DeleteArrayElementAtIndex(indexInParent);
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (nodeProp.isExpanded)
            {
                DrawNodeSpecificProperties(nodeProp, depth, isDirection);

                if (isDirection)
                {
                    var childrenProp = nodeProp.FindPropertyRelative("m_children");
                    if (childrenProp != null && childrenProp.isArray)
                    {
                        for (var i = 0; i < childrenProp.arraySize; i++)
                        {
                            var childProp = childrenProp.GetArrayElementAtIndex(i);
                            DrawNodeTreeRecursive(childProp, childrenProp, i, depth + 1);
                        }
                    }
                }
            }
        }

        private static void DrawCompactBaseSettings(SerializedProperty maxInstProp, SerializedProperty releaseDelayProp)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(new GUIContent("Max Inst", "Giới hạn số luồng âm thanh phát cùng lúc"), GUILayout.Width(55));
            var val1 = maxInstProp.intValue;
            var toggle1 = EditorGUILayout.Toggle(val1 > 0, GUILayout.Width(15));

            if (toggle1)
            {
                if (val1 <= 0) val1 = 1;
                maxInstProp.intValue = EditorGUILayout.IntField(val1, GUILayout.Width(30));
            }
            else
            {
                maxInstProp.intValue = 0;
                var disabledStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                GUILayout.Label("Tắt", disabledStyle, GUILayout.Width(30));
            }

            GUILayout.Space(15);

            GUILayout.Label(new GUIContent("Release", "Thời gian chờ xả RAM sau khi phát xong (0 = Xả ngay)"), GUILayout.Width(50));
            var val2 = releaseDelayProp.floatValue;
            var toggle2 = EditorGUILayout.Toggle(val2 > 0f, GUILayout.Width(15));

            if (toggle2)
            {
                if (val2 <= 0f) val2 = 15f;
                releaseDelayProp.floatValue = EditorGUILayout.FloatField(val2, GUILayout.Width(30));
                GUILayout.Label("s", EditorStyles.miniLabel, GUILayout.Width(10));
            }
            else
            {
                releaseDelayProp.floatValue = 0f;
                var disabledStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };
                GUILayout.Label("Ngay", disabledStyle, GUILayout.Width(40));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawNodeSpecificProperties(SerializedProperty nodeProp, int depth, bool isDirection)
        {
            var iterator = nodeProp.Copy();
            var endProperty = iterator.GetEndProperty();
            var enterChildren = true;
            var hasDrawnProperties = false;

            var maxInstProp = nodeProp.FindPropertyRelative("m_maxInstances");
            var releaseDelayProp = nodeProp.FindPropertyRelative("m_releaseDelay");
            var drawBaseSettings = !isDirection && maxInstProp != null && releaseDelayProp != null;

            while (iterator.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(iterator, endProperty)) break;
                enterChildren = false;

                if (iterator.name == "m_nodeName" || iterator.name == "m_children") continue;
                if (iterator.name == "m_maxInstances" || iterator.name == "m_releaseDelay") continue;

                if (!hasDrawnProperties)
                {
                    hasDrawnProperties = true;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space((depth + 1) * 15 + 22);
                    var boxStyle = new GUIStyle("helpbox") { padding = new RectOffset(10, 10, 10, 10) };
                    EditorGUILayout.BeginVertical(boxStyle);

                    if (drawBaseSettings)
                    {
                        DrawCompactBaseSettings(maxInstProp, releaseDelayProp);
                        EditorGUILayout.Space(5);
                        drawBaseSettings = false;
                    }
                }

                var isAudioRef = iterator.type == "AudioClipReference";
                if (isAudioRef)
                {
                    EditorGUILayout.BeginHorizontal();
                }

                EditorGUILayout.PropertyField(iterator, true);

                if (isAudioRef)
                {
                    var guidProp = iterator.FindPropertyRelative("m_AssetGUID");
                    if (guidProp != null && !string.IsNullOrEmpty(guidProp.stringValue))
                    {
                        if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton"), EditorStyles.miniButton, GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guidProp.stringValue);
                            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                            PlayEditorAudio(clip);
                        }

                        if (GUILayout.Button(EditorGUIUtility.IconContent("PreMatQuad"), EditorStyles.miniButton, GUILayout.Width(25), GUILayout.Height(18)))
                        {
                            StopEditorAudio();
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (!isAudioRef) EditorGUILayout.Space(2);
            }

            if (drawBaseSettings)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((depth + 1) * 15 + 22);
                var boxStyle = new GUIStyle("helpbox") { padding = new RectOffset(10, 10, 10, 10) };
                EditorGUILayout.BeginVertical(boxStyle);

                DrawCompactBaseSettings(maxInstProp, releaseDelayProp);

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            else if (hasDrawnProperties)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void ShowAddContextMenu(SerializedProperty directionNodeProp)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Direction"), false, () => AddChildNode(directionNodeProp, typeof(DirectionNode)));
            menu.AddSeparator("");

            var nodeTypes = TypeCache.GetTypesDerivedFrom<BaseAudioNode>();

            foreach (var type in nodeTypes)
            {
                if (type.IsAbstract || type == typeof(DirectionNode)) continue;

                var capturedType = type;
                var rawName = capturedType.Name;

                if (rawName.EndsWith("Node"))
                {
                    rawName = rawName.Substring(0, rawName.Length - 4);
                }

                var displayName = ObjectNames.NicifyVariableName(rawName);

                menu.AddItem(new GUIContent(displayName), false, () => AddChildNode(directionNodeProp, capturedType));
            }

            menu.ShowAsContext();
        }

        private static void AddChildNode(SerializedProperty directionNodeProp, Type nodeType)
        {
            var childrenProp = directionNodeProp.FindPropertyRelative("m_children");
            if (childrenProp == null) return;

            childrenProp.arraySize++;
            var newElement = childrenProp.GetArrayElementAtIndex(childrenProp.arraySize - 1);

            var newNode = (BaseAudioNode)Activator.CreateInstance(nodeType);
            newNode.WithName($"New {nodeType.Name}");

            newElement.managedReferenceValue = newNode;

            directionNodeProp.isExpanded = true;
            directionNodeProp.serializedObject.ApplyModifiedProperties();
        }

        private static string GetShortTypeName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return "Unknown";
            var parts = fullTypeName.Split(' ');
            if (parts.Length < 2) return fullTypeName;
            var namePath = parts[1].Split('.');
            return namePath[^1];
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

            GUILayout.Label("The UniAudio system requires the following packages to function:", EditorStyles.label);
            EditorGUILayout.Space(5);
            GUILayout.Label("• UniTask (Cysharp.Threading.Tasks)\n• Addressables (UnityEngine.AddressableAssets)", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();
        }
#endif
    }
}
#endif