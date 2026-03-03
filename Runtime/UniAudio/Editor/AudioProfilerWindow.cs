#if HAS_UNITASK && HAS_ADDRESSABLES
using System.Linq;
using UniCore.Audio;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.Audio
{
    public class AudioProfilerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _metricStyle;
        
        [MenuItem("UniCore/Windows/Audio Profiler", priority = 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioProfilerWindow>("Audio Profiler");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            _metricStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, richText = true };
        }

        private void Update()
        {
            if (Application.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label(new GUIContent(" Real-time Audio Profiler", EditorGUIUtility.IconContent("AudioMixerGroup Icon").image), _headerStyle);
            EditorGUILayout.Space(5);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(1)), new Color(0.5f, 0.5f, 0.5f, 0.4f));
            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Hãy vào Play Mode để giám sát Audio.", MessageType.Info);
                return;
            }

            if (AudioSystem.s_Instance == null)
            {
                EditorGUILayout.HelpBox("Hệ thống AudioSystem chưa được khởi tạo trong Scene.", MessageType.Warning);
                return;
            }

            DrawMetrics();
            EditorGUILayout.Space(15);
            DrawActiveEmitters();
        }

        private void DrawMetrics()
        {
            EditorGUILayout.BeginVertical(new GUIStyle("helpbox") { padding = new RectOffset(10, 10, 10, 10) });
            
            var activeEmittersCount = SoundEmitter.ActiveEmitters.Count;
            var culledCount = AudioSystem.TotalCulledCount;
            
            var cachedClipsCount = AudioClipReference.ActiveRefs.Count;
            float totalMemoryMB = 0f;
            var delayedReleaseCount = 0;

            foreach (var aRef in AudioClipReference.ActiveRefs)
            {
                var clip = aRef.GetLoadedClip();
                if (clip != null)
                {
                    totalMemoryMB += (clip.samples * clip.channels * 2) / 1048576f; 
                }
                
                if (aRef.IsDelayingRelease) delayedReleaseCount++;
            }

            GUILayout.Label($"<b>Active Emitters:</b> <color=#00FF00>{activeEmittersCount}</color>", _metricStyle);
            GUILayout.Label($"<b>Virtual Culling Drops:</b> <color=#FF5555>{culledCount}</color>", _metricStyle);
            EditorGUILayout.Space(5);
            GUILayout.Label($"<b>Addressables RAM Usage:</b> <color=#00FFFF>{totalMemoryMB:F2} MB</color> ({cachedClipsCount} Clips)", _metricStyle);
            GUILayout.Label($"<b>Clips pending LRU Auto-Release:</b> <color=#FFFF00>{delayedReleaseCount}</color>", _metricStyle);

            EditorGUILayout.EndVertical();
        }

        private void DrawActiveEmitters()
        {
            GUILayout.Label("Live Playing Voices", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, new GUIStyle("helpbox"));

            var emitters = SoundEmitter.ActiveEmitters.ToList();

            if (emitters.Count == 0)
            {
                GUILayout.Label("Không có âm thanh nào đang phát.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var emitter in emitters)
                {
                    if (emitter == null) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(EditorGUIUtility.IconContent("AudioClip Icon"), GUILayout.Width(20), GUILayout.Height(20));
                    GUILayout.Label(emitter.ClipName, EditorStyles.boldLabel);
                    
                    GUILayout.FlexibleSpace();
                    
                    var loopText = emitter.IsLooping ? "<color=#00FF00>[LOOP]</color>" : "";
                    GUILayout.Label($"Vol: {emitter.CurrentVolume:F2} {loopText}", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.EndHorizontal();
                    
                    var rect = GUILayoutUtility.GetRect(18, 18, "TextField");
                    EditorGUI.ProgressBar(rect, emitter.Progress, $"{(emitter.Progress * 100):F1}%");

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif