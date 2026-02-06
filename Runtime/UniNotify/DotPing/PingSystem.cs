using System;
using System.Collections.Generic;
using UniCore.Notify.DotPing.Internal;
using Newtonsoft.Json;
using UnityEngine;

namespace UniCore.Notify.DotPing
{
    public class PingSystem : MonoBehaviour
    {
        [SerializeField] private PingGraphic dotGraphic;
        [SerializeField] private PingGraphic upgradeGraphic;

        internal static PingLocationData locationData;
        internal static event Action<string, bool> OnPingChanged;
        private const string k_PrefsKey = "user.location_data";
        private static PingSystem instance;
        public static Transform GraphicParent() => instance.transform;

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        internal static void LoadData()
        {
            var json = PlayerPrefs.GetString(k_PrefsKey, null);
            locationData = (string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<PingLocationData>(json)) ?? new PingLocationData
            {
                nodes = new Dictionary<string, PingLocationNode>(15),
                childrenMap = new Dictionary<string, List<string>>(5)
            };
        }

        internal static PingGraphic CreateNewPingGraphic(GraphicType type)
        {
            return Instantiate(instance.GetPingPrefab(type), instance.transform);
        }

        internal PingGraphic GetPingPrefab(GraphicType type)
        {
            return type switch
            {
                GraphicType.Dot => dotGraphic,
                GraphicType.Upgrade => upgradeGraphic,
                _ => dotGraphic
            };
        }

        private static void NotifyPingChange(string locationId, bool isActive)
        {
            OnPingChanged?.Invoke(locationId, isActive);
        }

        public static void Push(string id, bool addIfContain = true)
        {
            var nodes = locationData.nodes;
            var childrenMap = locationData.childrenMap;
            if (nodes.ContainsKey(id))
            {
                var node = nodes[id];
                if (!addIfContain && node.value > 0) return;
                node.value++;
                nodes[id] = node;
                if (node.value != 1) return;
                NotifyPingChange(id, true);
                if (string.IsNullOrEmpty(node.parentId)) return;
                childrenMap[node.parentId].Add(id);
                UpdateParentPing(node.parentId);
                return;
            }

            var parentId = GetParentId(id);
            var newNode = new PingLocationNode
            {
                id = id,
                parentId = parentId,
                value = 1,
                hasChild = false
            };
            nodes[id] = newNode;
            NotifyPingChange(id, newNode.isActive);

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
            var nodes = locationData.nodes;
            var childrenMap = locationData.childrenMap;
            if (!nodes.TryGetValue(id, out var node)) return;
            node.value = forceHide ? 0 : node.value - 1;
            if (node.value <= 0)
            {
                if (node.hasChild) removeIfInactive = false;
                if (removeIfInactive)
                {
                    nodes.Remove(id);
                }
                else
                {
                    node.value = 0;
                    nodes[id] = node;
                }

                NotifyPingChange(id, false);
                if (string.IsNullOrEmpty(node.parentId)) return;
                if (childrenMap.TryGetValue(node.parentId, out var lisId)) lisId.Remove(id);
                UpdateParentPing(node.parentId);
                return;
            }

            nodes[id] = node;
        }

        public static bool IsActive(string id)
        {
            return locationData.nodes.ContainsKey(id) && locationData.nodes[id].isActive;
        }

        private static void UpdateParentPing(string id)
        {
            var isHasChildrenPing = false;
            if (locationData.childrenMap.TryGetValue(id, out var value))
            {
                for (var index = value.Count - 1; index >= 0; index--)
                {
                    var childId = value[index];
                    if (!locationData.nodes.ContainsKey(childId) || !locationData.nodes[childId].isActive) continue;
                    isHasChildrenPing = true;
                    break;
                }
            }

            if (isHasChildrenPing)
            {
                if (locationData.nodes.TryGetValue(id, out var node))
                {
                    node.hasChild = true;
                    NotifyPingChange(id, node.isActive);
                    locationData.nodes[id] = node;
                    return;
                }

                Push(id);
                var nodePush = locationData.nodes[id];
                nodePush.value = 0;
                nodePush.hasChild = true;
                NotifyPingChange(id, nodePush.isActive);
                locationData.nodes[id] = nodePush;
                return;
            }

            PopHasChild(id);
        }

        private static void PopHasChild(string id)
        {
            if (!locationData.nodes.TryGetValue(id, out var popNode)) return;
            popNode.hasChild = false;
            locationData.nodes[id] = popNode;
            NotifyPingChange(popNode.id, popNode.isActive);
            if (string.IsNullOrEmpty(popNode.parentId)) return;
            UpdateParentPing(popNode.parentId);
        }

        private static string GetParentId(string id)
        {
            var lastSlashIndex = id.LastIndexOf('/');
            return lastSlashIndex == -1 ? null : id[..lastSlashIndex];
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private void OnApplicationFocus(bool focus)
        {
            if (locationData == null) return;
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
            if (locationData == null) return;
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
            if (locationData == null) return;
            SaveData();
        }
#endif

        internal static void SaveData()
        {
            var json = JsonConvert.SerializeObject(locationData);
            PlayerPrefs.SetString(k_PrefsKey, json);
        }

        internal static void ClearData()
        {
            locationData = null;
            PlayerPrefs.DeleteKey(k_PrefsKey);
        }
    }
}