using System;
using System.Linq;
using UnityEditor;

namespace UniCore.Editor.QuickAccess
{
    internal static class QuickAccessFavorite
    {
        public static void RegisterUse(string guid)
        {
            var stat = QuickAccessStorage.Database().Stats.FirstOrDefault(s => s.GUID == guid);
            if (stat == null)
            {
                stat = new FavoriteStat { GUID = guid };
                QuickAccessStorage.Database().Stats.Add(stat);
            }

            stat.Score++;
            stat.LastUseTicks = DateTime.Now.Ticks;

            QuickAccessStorage.Database().Stats.RemoveAll(s => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(s.GUID)));

            QuickAccessStorage.Save(QuickAccessStorage.Database());
        }

        public static string[] GetFavorites()
        {
            return QuickAccessStorage.Database().Stats
                .OrderByDescending(GetWeight)
                .Take(QuickAccessStorage.Database().FavoriteLimit)
                .Select(s => s.GUID)
                .ToArray();
        }

        private static double GetWeight(FavoriteStat s)
        {
            var days = (DateTime.Now - new DateTime(s.LastUseTicks)).TotalDays;
            return s.Score * Math.Exp(-days * 0.15);
        }
    }
}