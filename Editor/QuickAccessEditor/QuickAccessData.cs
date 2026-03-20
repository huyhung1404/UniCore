using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace UniCore.Editor.QuickAccess
{
    [Serializable]
    public class QuickAccessDB
    {
        public int FavoriteLimit = 8;
        public List<GroupData> Groups = new();
        [NonSerialized, JsonIgnore] public List<FavoriteStat> Stats;
    }

    [Serializable]
    public class GroupData
    {
        public string GroupName;
        public bool GroupExpand = true;
        public List<AssetAddress> Assets = new();
    }

    [Serializable]
    public class AssetAddress
    {
        public string Name;
        public string GuidAsset;
    }

    [Serializable]
    public class FavoriteStat
    {
        public string GUID;
        public int Score;
        public long LastUseTicks;
    }
}