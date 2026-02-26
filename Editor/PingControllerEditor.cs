using System.Collections.Generic;
using UniCore.Notify.DotPing;
using UniCore.Notify.DotPing.Internal;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.Notify
{
    [CustomEditor(typeof(PingSystem))]
    internal class PingControllerEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private string _inputPush;
        private string _inputPop;
        private bool _forceHide;
        private bool _removeAtInactive;
        private static GUIStyle s_foldoutStyle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawLoad();
            var locationData = PingSystem.s_LocationData;
            GUILayout.Space(10);

            if (locationData?.Nodes == null || locationData.ChildrenMap == null)
            {
                EditorGUILayout.HelpBox("Need Load Location Data", MessageType.Warning);
                return;
            }

            GUILayout.Space(10);
            PushPop(locationData);
            GUILayout.Space(10);
            GUILayout.Label("Root");
            DrawTree(locationData);
        }

        private void PushPop(PingLocationData locationData)
        {
            _inputPush = EditorGUILayout.TextField(GUIContent.none, _inputPush);

            if (GUILayout.Button("Push"))
            {
                if (locationData == null)
                {
                    PingSystem.LoadData();
                }

                PingSystem.Push(_inputPush);
            }

            EditorGUILayout.Space(5);
            _inputPop = EditorGUILayout.TextField(GUIContent.none, _inputPop);
            _forceHide = EditorGUILayout.Toggle("Force Hide", _forceHide);
            _removeAtInactive = EditorGUILayout.Toggle("Remove At Inactive", _removeAtInactive);
            if (GUILayout.Button("Pop"))
            {
                if (locationData == null)
                {
                    PingSystem.LoadData();
                }

                PingSystem.Pop(_inputPop, _forceHide, _removeAtInactive);
            }
        }

        private static void DrawLoad()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load"))
            {
                PingSystem.LoadData();
            }

            if (GUILayout.Button("Save"))
            {
                PingSystem.SaveData();
            }

            if (GUILayout.Button("Clear"))
            {
                PingSystem.ClearData();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTree(PingLocationData locationData)
        {
            var nodes = locationData.Nodes;
            var childrenMap = new Dictionary<string, List<string>>();
            foreach (var node in locationData.ChildrenMap)
            {
                childrenMap.Add(node.Key, new List<string>(node.Value));
            }

            foreach (var locationDataNode in locationData.Nodes)
            {
                if (string.IsNullOrEmpty(locationDataNode.Value.ParentId)) continue;
                if (!childrenMap.TryGetValue(locationDataNode.Value.ParentId, out var children))
                {
                    childrenMap[locationDataNode.Value.ParentId] = new List<string> { locationDataNode.Key };
                    continue;
                }

                if (children.Contains(locationDataNode.Key)) continue;
                children.Add(locationDataNode.Key);
            }

            foreach (var rootNode in nodes)
            {
                if (!string.IsNullOrEmpty(rootNode.Value.ParentId)) continue;

                DrawNode(rootNode.Key, nodes, childrenMap, 1);
            }
        }

        private void DrawNode(string nodeId, Dictionary<string, PingLocationNode> nodes, Dictionary<string, List<string>> childrenMap, int depth)
        {
            if (!nodes.ContainsKey(nodeId)) return;
            _foldoutStates.TryAdd(nodeId, true);
            var node = nodes[nodeId];
            EditorGUI.indentLevel = depth;

            s_foldoutStyle ??= new GUIStyle(EditorStyles.foldout)
            {
                richText = true
            };

            _foldoutStates[nodeId] = EditorGUILayout.Foldout(_foldoutStates[nodeId],
                node.IsActive
                    ? $"<b><color=green>{node.Id}</color></b>  [{node.Value},{node.HasChild}]"
                    : $"{node.Id} [{node.Value}]  [{node.Value},{node.HasChild}]", s_foldoutStyle);
            if (!_foldoutStates[nodeId] || !childrenMap.TryGetValue(nodeId, out var children)) return;
            foreach (var childId in children)
            {
                DrawNode(childId, nodes, childrenMap, depth + 1);
            }
        }
    }
}