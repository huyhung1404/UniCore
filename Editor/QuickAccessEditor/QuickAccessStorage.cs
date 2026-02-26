using Newtonsoft.Json;
using UnityEditor;

namespace UniCore.Editor.QuickAccess
{
    internal static class QuickAccessStorage
    {
        public const string k_key = "QA_DB";
        private static QuickAccessDB s_db;

        public static QuickAccessDB Database()
        {
            s_db ??= Load();
            return s_db;
        }

        private static QuickAccessDB Load()
        {
            var json = EditorPrefs.GetString(k_key, "");
            return string.IsNullOrEmpty(json) ? new QuickAccessDB() : JsonConvert.DeserializeObject<QuickAccessDB>(json);
        }

        public static void Save(QuickAccessDB database)
        {
            EditorPrefs.SetString(k_key, JsonConvert.SerializeObject(database));
            s_db = database;
        }
    }
}