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
        private readonly Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        private string inputPush;
        private string inputPop;
        private bool forceHide;
        private bool removeAtInactive;
        private static GUIStyle foldoutStyle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawLoad();
            var locationData = PingSystem.locationData;
            GUILayout.Space(10);

            if (locationData?.nodes == null || locationData.childrenMap == null)
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
            inputPush = EditorGUILayout.TextField(GUIContent.none, inputPush);

            if (GUILayout.Button("Push"))
            {
                if (locationData == null)
                {
                    PingSystem.LoadData();
                }

                PingSystem.Push(inputPush);
            }

            EditorGUILayout.Space(5);
            inputPop = EditorGUILayout.TextField(GUIContent.none, inputPop);
            forceHide = EditorGUILayout.Toggle("Force Hide", forceHide);
            removeAtInactive = EditorGUILayout.Toggle("Remove At Inactive", removeAtInactive);
            if (GUILayout.Button("Pop"))
            {
                if (locationData == null)
                {
                    PingSystem.LoadData();
                }

                PingSystem.Pop(inputPop, forceHide, removeAtInactive);
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
            var nodes = locationData.nodes;
            var childrenMap = new Dictionary<string, List<string>>();
            foreach (var node in locationData.childrenMap)
            {
                childrenMap.Add(node.Key, new List<string>(node.Value));
            }

            foreach (var locationDataNode in locationData.nodes)
            {
                if (string.IsNullOrEmpty(locationDataNode.Value.parentId)) continue;
                if (!childrenMap.TryGetValue(locationDataNode.Value.parentId, out var children))
                {
                    childrenMap[locationDataNode.Value.parentId] = new List<string> { locationDataNode.Key };
                    continue;
                }

                if (children.Contains(locationDataNode.Key)) continue;
                children.Add(locationDataNode.Key);
            }

            foreach (var rootNode in nodes)
            {
                if (!string.IsNullOrEmpty(rootNode.Value.parentId)) continue;

                DrawNode(rootNode.Key, nodes, childrenMap, 1);
            }
        }

        private void DrawNode(string nodeId, Dictionary<string, PingLocationNode> nodes, Dictionary<string, List<string>> childrenMap, int depth)
        {
            if (!nodes.ContainsKey(nodeId)) return;
            foldoutStates.TryAdd(nodeId, true);
            var node = nodes[nodeId];
            EditorGUI.indentLevel = depth;

            foldoutStyle ??= new GUIStyle(EditorStyles.foldout)
            {
                richText = true
            };

            foldoutStates[nodeId] = EditorGUILayout.Foldout(foldoutStates[nodeId],
                node.isActive
                    ? $"<b><color=green>{node.id}</color></b>  [{node.value},{node.hasChild}]"
                    : $"{node.id} [{node.value}]  [{node.value},{node.hasChild}]", foldoutStyle);
            if (!foldoutStates[nodeId] || !childrenMap.TryGetValue(nodeId, out var children)) return;
            foreach (var childId in children)
            {
                DrawNode(childId, nodes, childrenMap, depth + 1);
            }
        }
    }
}