using System;
using System.Collections.Generic;
using System.IO;
using UniCore.Storage;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UniCore.Editor.PrefsEditor
{
    public class PrefsEditor : EditorWindow
    {
        private static readonly System.Text.Encoding s_encoding = new System.Text.UTF8Encoding();

        private struct PlayerPrefPair
        {
            public string Key { get; set; }
            public object Value { get; set; }
        }

        public enum PlayerPrefType
        {
            Float = 0,
            Int,
            String,
            Bool
        }

        private bool _showEditorPrefs;
        private List<PlayerPrefPair> _deserializedPlayerPrefs = new List<PlayerPrefPair>();
        private readonly List<PlayerPrefPair> _filteredPlayerPrefs = new List<PlayerPrefPair>();

        private DateTime? _lastDeserialization;
        private Vector2 _scrollPosition;
        private Vector2 _lastScrollPosition;
        private int _inspectorUpdateFrame;
        private bool _automaticDecryption = true;
        private string _searchFilter = string.Empty;
        private string _keyQueuedForDeletion;

        private PlayerPrefType _newEntryType = PlayerPrefType.String;
        private bool _newEntryIsEncrypted;
        private string _newEntryKey = string.Empty;
        private float _newEntryValueFloat;
        private int _newEntryValueInt;
        private bool _newEntryValueBool;
        private string _newEntryValueString = string.Empty;

        private SearchField _searchField;
        private GUIContent _deleteIcon;
        private GUIContent _settingsIcon;

        [MenuItem("UniCore/Windows/Prefs Editor", priority = 3)]
        private static void Init()
        {
            var editor = GetWindow<PrefsEditor>(false, "Prefs Editor");
            var minSize = editor.minSize;
            minSize.x = 450;
            editor.minSize = minSize;
            editor.Show();
        }

        private static string GetMacOSEditorPrefsPath()
        {
            const string fileName = "com.unity3d.UnityEditor5.x.plist";
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences", fileName);
        }

        private void OnEnable()
        {
            _searchField = new SearchField();
            _deserializedPlayerPrefs = new List<PlayerPrefPair>(RetrieveSavedPrefs(PlayerSettings.companyName, PlayerSettings.productName));

            _deleteIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
            if (_deleteIcon == null || _deleteIcon.image == null) _deleteIcon = new GUIContent("X", "Delete Pref");
            else _deleteIcon.tooltip = "Delete Pref";

            _settingsIcon = EditorGUIUtility.IconContent("_Popup");
            if (_settingsIcon == null || _settingsIcon.image == null) _settingsIcon = new GUIContent("⚙");

            UpdateSearch();
        }

        #region Wrapper Methods

        private void DeleteAll()
        {
            if (_showEditorPrefs) EditorPrefs.DeleteAll();
            else PlayerPrefs.DeleteAll();
        }

        private void DeleteKey(string key)
        {
            if (_showEditorPrefs) EditorPrefs.DeleteKey(key);
            else PlayerPrefs.DeleteKey(key);
        }

        private int GetInt(string key, int defaultValue = 0) => _showEditorPrefs ? EditorPrefs.GetInt(key, defaultValue) : PlayerPrefs.GetInt(key, defaultValue);
        private float GetFloat(string key, float defaultValue = 0.0f) => _showEditorPrefs ? EditorPrefs.GetFloat(key, defaultValue) : PlayerPrefs.GetFloat(key, defaultValue);

        private string GetString(string key, string defaultValue = "") =>
            _showEditorPrefs ? EditorPrefs.GetString(key, defaultValue) : PlayerPrefs.GetString(key, defaultValue);

        private bool GetBool(string key, bool defaultValue = false)
        {
            return !_showEditorPrefs ? throw new NotSupportedException("PlayerPrefs interface does not natively support bool") : EditorPrefs.GetBool(key, defaultValue);
        }

        private void SetInt(string key, int value)
        {
            if (_showEditorPrefs) EditorPrefs.SetInt(key, value);
            else PlayerPrefs.SetInt(key, value);
        }

        private void SetFloat(string key, float value)
        {
            if (_showEditorPrefs) EditorPrefs.SetFloat(key, value);
            else PlayerPrefs.SetFloat(key, value);
        }

        private void SetString(string key, string value)
        {
            if (_showEditorPrefs) EditorPrefs.SetString(key, value);
            else PlayerPrefs.SetString(key, value);
        }

        private void SetBool(string key, bool value)
        {
            if (!_showEditorPrefs) throw new NotSupportedException("PlayerPrefs interface does not natively support bools");
            EditorPrefs.SetBool(key, value);
        }

        private void Save()
        {
            if (!_showEditorPrefs) PlayerPrefs.Save();
        }

        #endregion

        private PlayerPrefPair[] RetrieveSavedPrefs(string companyName, string productName)
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var playerPrefsPath = _showEditorPrefs
                    ? GetMacOSEditorPrefsPath()
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences", $"unity.{companyName}.{productName}.plist");

                if (!File.Exists(playerPrefsPath)) return Array.Empty<PlayerPrefPair>();

                var plist = Plist.readPlist(playerPrefsPath);
                if (!(plist is Dictionary<string, object> parsed)) return Array.Empty<PlayerPrefPair>();

                var tempPlayerPrefs = new List<PlayerPrefPair>(parsed.Count);
                foreach (var kvp in parsed)
                {
                    switch (kvp.Value)
                    {
                        case double dValue:
                            tempPlayerPrefs.Add(new PlayerPrefPair { Key = kvp.Key, Value = (float)dValue });
                            break;
                        case bool: break;
                        default:
                            tempPlayerPrefs.Add(new PlayerPrefPair { Key = kvp.Key, Value = kvp.Value });
                            break;
                    }
                }

                return tempPlayerPrefs.ToArray();
            }

            if (Application.platform != RuntimePlatform.WindowsEditor) return Array.Empty<PlayerPrefPair>();

            var subKeyPath = _showEditorPrefs
                ? @"Software\Unity Technologies\Unity Editor 5.x"
                : $@"Software\Unity\UnityEditor\{companyName}\{productName}";

            var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKeyPath);
            if (registryKey == null) return Array.Empty<PlayerPrefPair>();

            var valueNames = registryKey.GetValueNames();
            var resultPairs = new PlayerPrefPair[valueNames.Length];

            for (var i = 0; i < valueNames.Length; i++)
            {
                var valueName = valueNames[i];
                var key = valueName;
                var index = key.LastIndexOf("_", StringComparison.Ordinal);
                if (index > 0) key = key.Remove(index, key.Length - index);

                var ambiguousValue = registryKey.GetValue(valueName);

                if (ambiguousValue is int or long)
                {
                    if (GetInt(key, -1) == -1 && GetInt(key) == 0) ambiguousValue = GetFloat(key);
                    else if (_showEditorPrefs && (!GetBool(key, true) || GetBool(key))) ambiguousValue = GetBool(key);
                }
                else if (ambiguousValue is byte[] bytes)
                {
                    ambiguousValue = s_encoding.GetString(bytes).TrimEnd('\0');
                }

                resultPairs[i] = new PlayerPrefPair { Key = key, Value = ambiguousValue };
            }

            return resultPairs;
        }

        private void UpdateSearch()
        {
            _filteredPlayerPrefs.Clear();
            if (string.IsNullOrEmpty(_searchFilter)) return;

            var lowerSearchFilter = _searchFilter.ToLower();

            foreach (var pref in _deserializedPlayerPrefs)
            {
                var fullKey = pref.Key;
                var displayKey = fullKey;

                if (_automaticDecryption && PlayerPrefsUtility.IsEncryptedKey(fullKey))
                    displayKey = PlayerPrefsUtility.DecryptKey(fullKey);

                if (displayKey.ToLower().Contains(lowerSearchFilter) || pref.Value.ToString().ToLower().Contains(lowerSearchFilter))
                    _filteredPlayerPrefs.Add(pref);
            }
        }

        private static void CalculateColumnRects(Rect rowRect, out Rect keyRect, out Rect valueRect, out Rect typeRect, out Rect rightRect, float rightOffset = 0f)
        {
            var activeRect = rowRect;
            activeRect.xMax -= rightOffset;

            rightRect = activeRect;
            rightRect.xMin = activeRect.xMax - 30;

            typeRect = rightRect;
            typeRect.x -= 45;
            typeRect.width = 45;

            keyRect = activeRect;
            keyRect.xMax = typeRect.xMin / 2f;

            valueRect = keyRect;
            valueRect.x += keyRect.width;

            keyRect.xMin += 5;
            keyRect.xMax -= 5;
            valueRect.xMin += 5;
            valueRect.xMax -= 5;
        }

        private void DrawTopBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            var oldIndex = _showEditorPrefs ? 1 : 0;
            var newIndex = GUILayout.Toolbar(oldIndex, new[] { "Player Prefs", "Editor Prefs" }, EditorStyles.toolbarButton, GUILayout.Width(200));

            if (newIndex != oldIndex)
            {
                _lastDeserialization = null;
                _showEditorPrefs = (newIndex == 1);
            }

            GUILayout.FlexibleSpace();

            var searchRect = GUILayoutUtility.GetRect(200, 300, 16, 16, EditorStyles.toolbarSearchField);
            var newSearchFilter = _searchField.OnToolbarGUI(searchRect, _searchFilter);

            if (newSearchFilter != _searchFilter)
            {
                _searchFilter = newSearchFilter;
                UpdateSearch();
            }

            if (GUILayout.Button(_settingsIcon, EditorStyles.toolbarButton, GUILayout.Width(35)))
            {
                ShowSystemOptionsMenu();
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawListHeader(bool hasScrollbar)
        {
            var headerRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.toolbar, GUILayout.Height(20), GUILayout.ExpandWidth(true));

            CalculateColumnRects(headerRect, out var keyRect, out var valueRect, out var typeRect, out var rightRect, hasScrollbar ? 14f : 0f);

            var headerStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleLeft };
            var centerStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };

            GUI.Label(keyRect, "Key", centerStyle);
            GUI.Label(valueRect, "Value", centerStyle);
            GUI.Label(typeRect, "Type", headerStyle);
            rightRect.x -= 3;
            GUI.Label(rightRect, "Del", headerStyle);
        }

        private void DrawMainList()
        {
            var activePlayerPrefs = string.IsNullOrEmpty(_searchFilter) ? _deserializedPlayerPrefs : _filteredPlayerPrefs;
            var entryCount = activePlayerPrefs.Count;

            if (entryCount == 0)
            {
                EditorGUILayout.HelpBox("No preferences found.", MessageType.Info);
                return;
            }

            const float baseRowHeight = 20;
            const float paddingEachSide = 2;
            const float rowHeight = baseRowHeight + paddingEachSide * 2;

            var availableHeight = position.height - 125f;
            var hasScrollbar = (entryCount * rowHeight) > availableHeight;

            DrawListHeader(hasScrollbar);

            var textFieldStyle = new GUIStyle(GUI.skin.textField);

            _lastScrollPosition = _scrollPosition;
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            if (_scrollPosition.y < 0) _scrollPosition.y = 0;

            var visibleCount = Mathf.CeilToInt(Screen.height / rowHeight);
            var firstShownIndex = Mathf.FloorToInt(_scrollPosition.y / rowHeight);
            var shownIndexLimit = Mathf.Min(entryCount, firstShownIndex + visibleCount);

            if (shownIndexLimit - firstShownIndex < visibleCount)
            {
                firstShownIndex = Mathf.Max(0, shownIndexLimit - visibleCount);
            }

            GUILayout.Space(firstShownIndex * rowHeight);

            for (var i = firstShownIndex; i < shownIndexLimit; i++)
            {
                var rowRect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(rowHeight));

                var rowColor = EditorGUIUtility.isProSkin
                    ? (i % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.25f, 0.25f, 0.25f))
                    : (i % 2 == 0 ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.95f, 0.95f, 0.95f));
                EditorGUI.DrawRect(rowRect, rowColor);

                rowRect.y += paddingEachSide;
                rowRect.height = baseRowHeight;

                CalculateColumnRects(rowRect, out var keyRect, out var valueRect, out var typeRect, out var rightRect);

                var isEncryptedPair = PlayerPrefsUtility.IsEncryptedKey(activePlayerPrefs[i].Key);
                UpdateTextFieldStyleColor(textFieldStyle, isEncryptedPair);

                var fullKey = activePlayerPrefs[i].Key;
                var displayKey = fullKey;
                var deserializedValue = activePlayerPrefs[i].Value;
                var failedAutoDecrypt = false;

                if (isEncryptedPair && _automaticDecryption)
                {
                    try
                    {
                        deserializedValue = PlayerPrefsUtility.GetEncryptedValue(fullKey, deserializedValue?.ToString());
                        displayKey = PlayerPrefsUtility.DecryptKey(fullKey);
                    }
                    catch
                    {
                        textFieldStyle.normal.textColor = Color.red;
                        textFieldStyle.focused.textColor = Color.red;
                        failedAutoDecrypt = true;
                    }
                }

                var valueType = GetValueType(isEncryptedPair, failedAutoDecrypt, fullKey, deserializedValue);

                EditorGUI.TextField(keyRect, displayKey, textFieldStyle);
                DrawValueField(valueRect, typeRect, textFieldStyle, isEncryptedPair, failedAutoDecrypt, valueType, fullKey, displayKey,
                    initialValueStr: deserializedValue?.ToString());

                if (GUI.Button(rightRect, _deleteIcon, EditorStyles.miniButton))
                {
                    DeleteKey(fullKey);
                    Save();
                    DeleteCachedRecord(fullKey);
                }
            }

            var bottomPadding = (entryCount - shownIndexLimit) * rowHeight;
            if (bottomPadding > 0) GUILayout.Space(bottomPadding);

            EditorGUILayout.EndScrollView();
        }

        private void DrawCompactAddEntry()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            var targetPrefs = _showEditorPrefs ? "Editor Prefs" : "Player Prefs";
            GUILayout.Label($"Add to {targetPrefs}", EditorStyles.boldLabel, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            var typeNames = _showEditorPrefs ? new[] { "Float", "Int", "String", "Bool" } : new[] { "Float", "Int", "String" };
            var currentIndex = (int)_newEntryType;
            if (!_showEditorPrefs && currentIndex == 3) currentIndex = 2;

            currentIndex = GUILayout.Toolbar(currentIndex, typeNames, EditorStyles.miniButton, GUILayout.Width(200));
            _newEntryType = (PlayerPrefType)currentIndex;

            GUILayout.Space(10);

            var lockIcon = _newEntryIsEncrypted ? "Encrypted" : "Unencrypted";
            var toggleStyle = new GUIStyle(EditorStyles.miniButton) { fixedWidth = 100 };
            _newEntryIsEncrypted = GUILayout.Toggle(_newEntryIsEncrypted, lockIcon, toggleStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUILayout.Label("Key", EditorStyles.label, GUILayout.Width(25));
            GUI.SetNextControlName("newEntryKey");
            _newEntryKey = EditorGUILayout.TextField(_newEntryKey);

            GUILayout.Label("Value", EditorStyles.label, GUILayout.Width(35));
            GUI.SetNextControlName("newEntryValue");
            switch (_newEntryType)
            {
                case PlayerPrefType.Float: _newEntryValueFloat = EditorGUILayout.FloatField(_newEntryValueFloat); break;
                case PlayerPrefType.Int: _newEntryValueInt = EditorGUILayout.IntField(_newEntryValueInt); break;
                case PlayerPrefType.Bool: _newEntryValueBool = EditorGUILayout.Toggle(_newEntryValueBool, GUILayout.Width(40)); break;
                case PlayerPrefType.String: _newEntryValueString = EditorGUILayout.TextField(_newEntryValueString); break;
            }

            var isEnterPressed = Event.current.isKey && Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyUp &&
                                 (GUI.GetNameOfFocusedControl() == "newEntryKey" || GUI.GetNameOfFocusedControl() == "newEntryValue");

            if ((GUILayout.Button("Add", GUILayout.Width(60)) || isEnterPressed) && !string.IsNullOrEmpty(_newEntryKey))
            {
                AddNewEntry();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUILayout.EndVertical();
        }

        private void DrawBottomStatusBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();

            var activePlayerPrefs = string.IsNullOrEmpty(_searchFilter) ? _deserializedPlayerPrefs : _filteredPlayerPrefs;
            GUILayout.Label($"Total Entries: {activePlayerPrefs.Count}", EditorStyles.miniLabel);

            GUILayout.EndHorizontal();
        }

        private void ShowSystemOptionsMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Auto-Decryption"), _automaticDecryption, () => _automaticDecryption = !_automaticDecryption);

            if (!SimpleEncryption.IsCustomKeyApplied)
            {
                menu.AddItem(new GUIContent("Encryption/Generate Custom Key Script"), false, GenerateCustomKeyScript);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Encryption/Custom Key is Active"));
            }

            menu.AddSeparator("");

            if (!_showEditorPrefs)
            {
                menu.AddItem(new GUIContent("Import PlayerPrefs..."), false, () => ScriptableWizard.DisplayWizard<ImportPrefsWizard>("Import PlayerPrefs", "Import"));
            }

            menu.AddItem(new GUIContent("Force Save"), false, Save);
            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Delete All Preferences"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete All?", "Are you sure you want to delete all preferences?", "Delete All", "Cancel"))
                {
                    DeleteAll();
                    Save();
                    _deserializedPlayerPrefs.Clear();
                    UpdateSearch();
                }
            });

            menu.ShowAsContext();
        }

        private Type GetValueType(bool isEncryptedPair, bool failedAutoDecrypt, string fullKey, object deserializedValue)
        {
            if (!isEncryptedPair || !_automaticDecryption || failedAutoDecrypt) return deserializedValue?.GetType() ?? typeof(string);

            var encryptedValue = GetString(fullKey);
            if (encryptedValue.StartsWith(PlayerPrefsUtility.VALUE_FLOAT_PREFIX)) return typeof(float);
            if (encryptedValue.StartsWith(PlayerPrefsUtility.VALUE_INT_PREFIX)) return typeof(int);
            if (encryptedValue.StartsWith(PlayerPrefsUtility.VALUE_BOOL_PREFIX)) return typeof(bool);
            if (encryptedValue.StartsWith(PlayerPrefsUtility.VALUE_STRING_PREFIX) || string.IsNullOrEmpty(encryptedValue)) return typeof(string);

            throw new InvalidOperationException("Could not decrypt item, no match found in known encrypted key prefixes");
        }

        private void UpdateTextFieldStyleColor(GUIStyle style, bool isEncrypted)
        {
            if (!isEncrypted)
            {
                style.normal.textColor = GUI.skin.textField.normal.textColor;
                style.focused.textColor = GUI.skin.textField.focused.textColor;
                return;
            }

            var color = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 1) : new Color(0, 0, 1);
            style.normal.textColor = color;
            style.focused.textColor = color;
        }

        private void DrawValueField(Rect valueRect, Rect typeRect, GUIStyle style, bool isEncrypted, bool failedAutoDecrypt, Type valueType, string fullKey, string displayKey,
            string initialValueStr)
        {
            var labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

            if (valueType == typeof(float))
            {
                var initialValue = (isEncrypted && _automaticDecryption) ? PlayerPrefsUtility.GetEncryptedFloat(displayKey) : GetFloat(fullKey);
                var newValue = EditorGUI.FloatField(valueRect, initialValue, style);

                if (!Mathf.Approximately(newValue, initialValue))
                {
                    if (isEncrypted) SetString(fullKey, PlayerPrefsUtility.VALUE_FLOAT_PREFIX + SimpleEncryption.EncryptFloat(newValue));
                    else SetFloat(fullKey, newValue);
                    Save();
                }

                GUI.Label(typeRect, "float", labelStyle);
            }
            else if (valueType == typeof(int))
            {
                var initialValue = (isEncrypted && _automaticDecryption) ? PlayerPrefsUtility.GetEncryptedInt(displayKey) : GetInt(fullKey);
                var newValue = EditorGUI.IntField(valueRect, initialValue, style);

                if (newValue != initialValue)
                {
                    if (isEncrypted) SetString(fullKey, PlayerPrefsUtility.VALUE_INT_PREFIX + SimpleEncryption.EncryptInt(newValue));
                    else SetInt(fullKey, newValue);
                    Save();
                }

                GUI.Label(typeRect, "int", labelStyle);
            }
            else if (valueType == typeof(bool))
            {
                var initialValue = (isEncrypted && _automaticDecryption) ? PlayerPrefsUtility.GetEncryptedBool(displayKey) : GetBool(fullKey);
                var newValue = EditorGUI.Toggle(valueRect, initialValue);

                if (newValue != initialValue)
                {
                    if (isEncrypted) SetString(fullKey, PlayerPrefsUtility.VALUE_BOOL_PREFIX + SimpleEncryption.EncryptBool(newValue));
                    else SetBool(fullKey, newValue);
                    Save();
                }

                GUI.Label(typeRect, "bool", labelStyle);
            }
            else if (valueType == typeof(string))
            {
                var initialValue = (isEncrypted && _automaticDecryption && !failedAutoDecrypt) ? PlayerPrefsUtility.GetEncryptedString(displayKey) : GetString(fullKey);

                var stringRect = valueRect;
                var btnRect = new Rect(stringRect.xMax - 25, stringRect.y, 25, stringRect.height);
                stringRect.width -= 27;

                var newValue = EditorGUI.TextField(stringRect, initialValue, style);

                if (newValue != initialValue && !failedAutoDecrypt)
                {
                    if (isEncrypted) SetString(fullKey, PlayerPrefsUtility.VALUE_STRING_PREFIX + SimpleEncryption.EncryptString(newValue));
                    else SetString(fullKey, newValue);
                    Save();
                }

                if (GUI.Button(btnRect, "...", EditorStyles.miniButton))
                {
                    StringViewerWindow.ShowWindow(displayKey, initialValue, (updatedValue) =>
                    {
                        if (isEncrypted) SetString(fullKey, PlayerPrefsUtility.VALUE_STRING_PREFIX + SimpleEncryption.EncryptString(updatedValue));
                        else SetString(fullKey, updatedValue);

                        CacheRecord(fullKey, updatedValue);
                        Save();
                        Repaint();
                    });
                }

                if (isEncrypted && !_automaticDecryption && !string.IsNullOrEmpty(initialValueStr))
                {
                    var playerPrefType = (PlayerPrefType)(int)char.GetNumericValue(initialValueStr[0]);
                    GUI.Label(typeRect, playerPrefType.ToString().ToLower(), labelStyle);
                }
                else
                {
                    GUI.Label(typeRect, "string", labelStyle);
                }
            }
            else
            {
                GUI.Label(valueRect, "Unsupported", labelStyle);
            }
        }

        private void AddNewEntry()
        {
            if (_newEntryIsEncrypted)
            {
                var encryptedKey = PlayerPrefsUtility.KEY_PREFIX + SimpleEncryption.EncryptString(_newEntryKey);
                var encryptedValue = _newEntryType switch
                {
                    PlayerPrefType.Float => PlayerPrefsUtility.VALUE_FLOAT_PREFIX + SimpleEncryption.EncryptFloat(_newEntryValueFloat),
                    PlayerPrefType.Int => PlayerPrefsUtility.VALUE_INT_PREFIX + SimpleEncryption.EncryptInt(_newEntryValueInt),
                    PlayerPrefType.Bool => PlayerPrefsUtility.VALUE_BOOL_PREFIX + SimpleEncryption.EncryptBool(_newEntryValueBool),
                    _ => PlayerPrefsUtility.VALUE_STRING_PREFIX + SimpleEncryption.EncryptString(_newEntryValueString)
                };

                SetString(encryptedKey, encryptedValue);
                CacheRecord(encryptedKey, encryptedValue);
            }
            else
            {
                switch (_newEntryType)
                {
                    case PlayerPrefType.Float:
                        SetFloat(_newEntryKey, _newEntryValueFloat);
                        CacheRecord(_newEntryKey, _newEntryValueFloat);
                        break;
                    case PlayerPrefType.Int:
                        SetInt(_newEntryKey, _newEntryValueInt);
                        CacheRecord(_newEntryKey, _newEntryValueInt);
                        break;
                    case PlayerPrefType.Bool:
                        SetBool(_newEntryKey, _newEntryValueBool);
                        CacheRecord(_newEntryKey, _newEntryValueBool);
                        break;
                    default:
                        SetString(_newEntryKey, _newEntryValueString);
                        CacheRecord(_newEntryKey, _newEntryValueString);
                        break;
                }
            }

            Save();
            Repaint();

            _newEntryKey = string.Empty;
            _newEntryValueFloat = 0;
            _newEntryValueInt = 0;
            _newEntryValueString = string.Empty;
            GUI.FocusControl("");
        }

        private static void GenerateCustomKeyScript()
        {
            var templateText = @"
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UniCore.Storage;

public static class GamePrefsEncryptionKeyInitializer
{
    private static readonly byte[] customKey = {#CUSTOMKEY#};

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
#endif
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        SimpleEncryption.SetCustomKey(customKey);
    }
}
";
            var key = new byte[32];
            for (var i = 0; i < 32; i++) key[i] = (byte)Random.Range(0, 256);

            templateText = templateText.Replace("#CUSTOMKEY#", string.Join(", ", key));
            File.WriteAllText("Assets/GamePrefsEncryptionKeyInitializer.cs", templateText);
            AssetDatabase.ImportAsset("Assets/GamePrefsEncryptionKeyInitializer.cs", ImportAssetOptions.ForceUpdate);
        }

        private void DeserializePrefsIntoCache()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var playerPrefsPath = _showEditorPrefs
                    ? GetMacOSEditorPrefsPath()
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences",
                        $"unity.{PlayerSettings.companyName}.{PlayerSettings.productName}.plist");

                var lastWriteTime = File.GetLastWriteTimeUtc(playerPrefsPath);

                if (!_lastDeserialization.HasValue || _lastDeserialization.Value != lastWriteTime)
                {
                    _deserializedPlayerPrefs = new List<PlayerPrefPair>(RetrieveSavedPrefs(PlayerSettings.companyName, PlayerSettings.productName));
                    _lastDeserialization = lastWriteTime;
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                if (!_lastDeserialization.HasValue || DateTime.UtcNow - _lastDeserialization.Value > TimeSpan.FromMilliseconds(500))
                {
                    _deserializedPlayerPrefs = new List<PlayerPrefPair>(RetrieveSavedPrefs(PlayerSettings.companyName, PlayerSettings.productName));
                    _lastDeserialization = DateTime.UtcNow;
                }
            }
        }

        private void OnGUI()
        {
            DrawTopBar();

            DeserializePrefsIntoCache();
            DrawMainList();

            DrawCompactAddEntry();
            DrawBottomStatusBar();

            if (_scrollPosition != _lastScrollPosition)
            {
                GUI.FocusControl("");
            }
        }

        private void CacheRecord(string key, object value)
        {
            var replaced = false;
            for (var i = 0; i < _deserializedPlayerPrefs.Count; i++)
            {
                if (_deserializedPlayerPrefs[i].Key == key)
                {
                    _deserializedPlayerPrefs[i] = new PlayerPrefPair { Key = key, Value = value };
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                _deserializedPlayerPrefs.Add(new PlayerPrefPair { Key = key, Value = value });
            }

            UpdateSearch();
        }

        private void DeleteCachedRecord(string fullKey) => _keyQueuedForDeletion = fullKey;

        private void OnInspectorUpdate()
        {
            if (!string.IsNullOrEmpty(_keyQueuedForDeletion))
            {
                if (_deserializedPlayerPrefs != null)
                {
                    for (var i = 0; i < _deserializedPlayerPrefs.Count; i++)
                    {
                        if (_deserializedPlayerPrefs[i].Key == _keyQueuedForDeletion)
                        {
                            _deserializedPlayerPrefs.RemoveAt(i);
                            break;
                        }
                    }
                }

                _keyQueuedForDeletion = null;
                UpdateSearch();
                Repaint();
            }
            else if (_inspectorUpdateFrame % 10 == 0)
            {
                Repaint();
            }

            _inspectorUpdateFrame++;
        }

        public void Import(string companyName, string productName)
        {
            var importedPairs = RetrieveSavedPrefs(companyName, productName);

            foreach (var pair in importedPairs)
            {
                var type = pair.Value.GetType();

                if (type == typeof(float)) SetFloat(pair.Key, (float)pair.Value);
                else if (type == typeof(int)) SetInt(pair.Key, (int)pair.Value);
                else if (type == typeof(string)) SetString(pair.Key, (string)pair.Value);

                CacheRecord(pair.Key, pair.Value);
            }

            Save();
        }
    }

    public class StringViewerWindow : EditorWindow
    {
        private string _key;
        private string _text;
        private Action<string> _onSave;
        private Vector2 _scroll;

        public static void ShowWindow(string key, string text, Action<string> onSave)
        {
            var win = GetWindow<StringViewerWindow>(true, "String Editor", true);
            win._key = key;
            win._text = text;
            win._onSave = onSave;
            win.minSize = new Vector2(400, 300);
            win.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label($"Key: {_key}", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, EditorStyles.helpBox);

            var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _text = EditorGUILayout.TextArea(_text, style, GUILayout.ExpandHeight(true));

            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(25)))
            {
                Close();
            }

            if (GUILayout.Button("Save & Close", GUILayout.Width(100), GUILayout.Height(25)))
            {
                _onSave?.Invoke(_text);
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }
    }
}