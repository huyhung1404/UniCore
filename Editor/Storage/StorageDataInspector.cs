using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UniCore.Storage;

namespace UniCore.Editor.Storage
{
    public class StorageDataInspector : EditorWindow
    {
        private string _filePath;
        private string _jsonData = "";
        private Vector2 _scrollPos;
        private bool _isDirty;
        private string _statusMessage = "Ready";
        private MessageType _statusType = MessageType.Info;

        [MenuItem("UniCore/Windows/Storage Data Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<StorageDataInspector>("Storage Inspector");
            window.minSize = new Vector2(600, 500);
        }

        private void OnGUI()
        {
            DrawHeader();

            EditorGUILayout.Space(10);

            DrawFileSelector();

            EditorGUILayout.Space(10);

            DrawJsonArea();

            DrawFooter();
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };

            EditorGUILayout.LabelField("UniCore Save Data Inspector", headerStyle);
            EditorGUILayout.HelpBox("This tool allows you to decrypt, edit, and re-encrypt save files using the current Project Settings.", MessageType.Warning);
        }

        private void DrawFileSelector()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("File Selection", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Target File:", _filePath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFilePanel("Select Save File", Application.persistentDataPath, "dat");
                if (!string.IsNullOrEmpty(path)) _filePath = path;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Load & Decrypt", EditorGUIUtility.IconContent("d_Record Next").image)))
            {
                DecryptAndLoad();
            }

            if (GUILayout.Button(new GUIContent(" Open Persistent Path", EditorGUIUtility.IconContent("d_Folder opened").image)))
            {
                EditorUtility.RevealInFinder(Application.persistentDataPath);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawJsonArea()
        {
            EditorGUILayout.LabelField($"Data Content {(_isDirty ? "*" : "")}", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUI.BeginChangeCheck();
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
            {
                font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UniCore/Editor/Fonts/JetBrainsMono.ttf"), // Optional: Mono font if you have one
                wordWrap = true,
                richText = true
            };

            _jsonData = EditorGUILayout.TextArea(_jsonData, textAreaStyle, GUILayout.ExpandHeight(true));

            if (EditorGUI.EndChangeCheck())
            {
                _isDirty = true;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = _isDirty && !string.IsNullOrEmpty(_jsonData);
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("SAVE & RE-ENCRYPT", GUILayout.Height(40)))
            {
                EncryptAndSave();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            if (GUILayout.Button("Clear Content", GUILayout.Height(40), GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Clear Content", "Are you sure you want to clear the editor area?", "Yes", "No"))
                {
                    _jsonData = "";
                    _isDirty = false;
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void DecryptAndLoad()
        {
            if (!File.Exists(_filePath))
            {
                SetStatus("File not found!", MessageType.Error);
                return;
            }

            try
            {
                var data = File.ReadAllBytes(_filePath);
                var settings = StorageSettings.GetInstance(StorageSettings.k_FileName);

                var raw = settings.Protector.Unprotect(data);

                raw = settings.Encryptor.Decrypt(raw);

                raw = settings.Compressor.Decompress(raw);

                _jsonData = Encoding.UTF8.GetString(raw);

                try
                {
                    var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(_jsonData);
                    _jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
                }
                catch
                {
                    /* Not a JSON or already formatted */
                }

                _isDirty = false;
                SetStatus("Decryption Successful!", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"Decryption Failed: {e.Message}", MessageType.Error);
                Debug.LogException(e);
            }
        }

        private void EncryptAndSave()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                SetStatus("Please select a file path first.", MessageType.Warning);
                return;
            }

            var raw = Encoding.UTF8.GetBytes(_jsonData);
            try
            {
                var settings = StorageSettings.GetInstance(StorageSettings.k_FileName);

                raw = settings.Compressor.Compress(raw);

                raw = settings.Encryptor.Encrypt(raw);

                var protectedData = settings.Protector.Protect(raw);

                File.WriteAllBytes(_filePath, protectedData);

                _isDirty = false;
                SetStatus($"File saved and encrypted at {DateTime.Now.ToShortTimeString()}", MessageType.Info);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                SetStatus($"Encryption Failed: {e.Message}", MessageType.Error);
                Debug.LogException(e);
            }
        }

        private void SetStatus(string msg, MessageType type)
        {
            _statusMessage = msg;
            _statusType = type;
        }
    }
}