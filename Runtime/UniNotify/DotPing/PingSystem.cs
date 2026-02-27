using System;
using System.Collections.Generic;
using UniCore.Notify.DotPing.Internal;
using Newtonsoft.Json;
using UnityEngine;

namespace UniCore.Notify.DotPing
{
    public class PingSystem : MonoBehaviour
    {
        [SerializeField] private PingGraphic m_dotGraphic;
        [SerializeField] private PingGraphic m_upgradeGraphic;

        internal static PingLocationData s_LocationData;
        internal static event Action<string, bool> OnPingChanged;
        private const string k_prefsKey = "user.location_data";
        private static PingSystem s_instance;
        public static Transform GraphicParent() => s_instance.transform;

        private void Awake()
        {
            s_instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        internal static void LoadData()
        {
            var json = PlayerPrefs.GetString(k_prefsKey, null);
            s_LocationData = (string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<PingLocationData>(json)) ?? new PingLocationData
            {
                Nodes = new Dictionary<string, PingLocationNode>(15),
                ChildrenMap = new Dictionary<string, List<string>>(5)
            };
        }

        internal static PingGraphic CreateNewPingGraphic(GraphicType type)
        {
            return Instantiate(s_instance.GetPingPrefab(type), s_instance.transform);
        }

        internal PingGraphic GetPingPrefab(GraphicType type)
        {
            return type switch
            {
                GraphicType.Dot => m_dotGraphic,
                GraphicType.Upgrade => m_upgradeGraphic,
                _ => m_dotGraphic
            };
        }

        private static void NotifyPingChange(string locationId, bool isActive)
        {
            OnPingChanged?.Invoke(locationId, isActive);
        }

        public static void Push(string id, bool addIfContain = true)
        {
            var nodes = s_LocationData.Nodes;
            var childrenMap = s_LocationData.ChildrenMap;
            if (nodes.ContainsKey(id))
            {
                var node = nodes[id];
                if (!addIfContain && node.Value > 0) return;
                node.Value++;
                nodes[id] = node;
                if (node.Value != 1) return;
                NotifyPingChange(id, true);
                if (string.IsNullOrEmpty(node.ParentId)) return;
                childrenMap[node.ParentId].Add(id);
                UpdateParentPing(node.ParentId);
                return;
            }

            var parentId = GetParentId(id);
            var newNode = new PingLocationNode
            {
                Id = id,
                ParentId = parentId,
                Value = 1,
                HasChild = false
            };
            nodes[id] = newNode;
            NotifyPingChange(id, newNode.IsActive);

            if (string.IsNullOrEmpty(parentId)) return;
            if (!childrenMap.ContainsKey(parentId))
            {
                childrenMap[parentId] = new List<string>();
            }

            childrenMap[parentId].Add(id);
            UpdateParentPing(parentId);
        }

        public static void Pop(string id, bool forceHide = false, bool removeIfInactive = false)
        {
            var nodes = s_LocationData.Nodes;
            var childrenMap = s_LocationData.ChildrenMap;
            if (!nodes.TryGetValue(id, out var node)) return;
            node.Value = forceHide ? 0 : node.Value - 1;
            if (node.Value <= 0)
            {
                if (node.HasChild) removeIfInactive = false;
                if (removeIfInactive)
                {
                    nodes.Remove(id);
                }
                else
                {
                    node.Value = 0;
                    nodes[id] = node;
                }

                NotifyPingChange(id, false);
                if (string.IsNullOrEmpty(node.ParentId)) return;
                if (childrenMap.TryGetValue(node.ParentId, out var lisId)) lisId.Remove(id);
                UpdateParentPing(node.ParentId);
                return;
            }

            nodes[id] = node;
        }

        public static bool IsActive(string id)
        {
            return s_LocationData.Nodes.ContainsKey(id) && s_LocationData.Nodes[id].IsActive;
        }

        private static void UpdateParentPing(string id)
        {
            var isHasChildrenPing = false;
            if (s_LocationData.ChildrenMap.TryGetValue(id, out var value))
            {
                for (var index = value.Count - 1; index >= 0; index--)
                {
                    var childId = value[index];
                    if (!s_LocationData.Nodes.ContainsKey(childId) || !s_LocationData.Nodes[childId].IsActive) continue;
                    isHasChildrenPing = true;
                    break;
                }
            }

            if (isHasChildrenPing)
            {
                if (s_LocationData.Nodes.TryGetValue(id, out var node))
                {
                    node.HasChild = true;
                    NotifyPingChange(id, node.IsActive);
                    s_LocationData.Nodes[id] = node;
                    return;
                }

                Push(id);
                var nodePush = s_LocationData.Nodes[id];
                nodePush.Value = 0;
                nodePush.HasChild = true;
                NotifyPingChange(id, nodePush.IsActive);
                s_LocationData.Nodes[id] = nodePush;
                return;
            }

            PopHasChild(id);
        }

        private static void PopHasChild(string id)
        {
            if (!s_LocationData.Nodes.TryGetValue(id, out var popNode)) return;
            popNode.HasChild = false;
            s_LocationData.Nodes[id] = popNode;
            NotifyPingChange(popNode.Id, popNode.IsActive);
            if (string.IsNullOrEmpty(popNode.ParentId)) return;
            UpdateParentPing(popNode.ParentId);
        }

        private static string GetParentId(string id)
        {
            var lastSlashIndex = id.LastIndexOf('/');
            return lastSlashIndex == -1 ? null : id[..lastSlashIndex];
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (s_LocationData == null) return;
            if (focus)
            {
                return;
            }

            SaveData();
        }
#endif

#if UNITY_EDITOR || UNITY_IOS
        private void OnApplicationPause(bool pauseStatus)
        {
            if (s_LocationData == null) return;
            if (pauseStatus)
            {
#if !UNITY_EDITOR
                SaveData();
#endif
            }
        }
#endif

#if UNITY_EDITOR
        private void OnApplicationQuit()
        {
            if (s_LocationData == null) return;
            SaveData();
        }
#endif

        internal static void SaveData()
        {
            var json = JsonConvert.SerializeObject(s_LocationData);
            PlayerPrefs.SetString(k_prefsKey, json);
        }

        internal static void ClearData()
        {
            s_LocationData = null;
            PlayerPrefs.DeleteKey(k_prefsKey);
        }
    }
}