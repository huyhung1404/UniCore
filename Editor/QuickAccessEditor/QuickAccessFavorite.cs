using System;
using System.Linq;
using UnityEditor;

namespace UniCore.Editor.QuickAccess
{
    internal static class QuickAccessFavorite
    {
        public static void RegisterUse(string guid)
        {
            var stat = EditorStorage.Database().Stats.FirstOrDefault(s => s.GUID == guid);
            if (stat == null)
            {
                stat = new FavoriteStat { GUID = guid };
                EditorStorage.Database().Stats.Add(stat);
            }

            stat.Score++;
            stat.LastUseTicks = DateTime.Now.Ticks;

            EditorStorage.Database().Stats.RemoveAll(s => string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(s.GUID)));

            EditorStorage.Save(EditorStorage.Database());
        }

        public static string[] GetFavorites()
        {
            return EditorStorage.Database().Stats
                .OrderByDescending(GetWeight)
                .Take(EditorStorage.Database().FavoriteLimit)
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