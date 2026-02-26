using System;
using System.Collections.Generic;

namespace UniCore.Editor.QuickAccess
{
    [Serializable]
    public class QuickAccessDB
    {
        public int FavoriteLimit = 8;
        public List<GroupData> Groups = new();
        public List<FavoriteStat> Stats = new();
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