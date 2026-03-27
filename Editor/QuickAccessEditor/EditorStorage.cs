using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.QuickAccess
{
    [FilePath("ProjectSettings/UniCore_Editor_QuickAccess.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class EditorStorage : ScriptableSingleton<EditorStorage>
    {
        private const string k_statsKey = "UniCore_Editor_QuickAccess_Stats";
        [SerializeField] private QuickAccessDB _database;

        public static QuickAccessDB Database()
        {
            instance._database ??= new QuickAccessDB();
            instance._database.Stats ??= EditorPrefs.HasKey(k_statsKey) ? 
                JsonConvert.DeserializeObject<List<FavoriteStat>>(EditorPrefs.GetString(k_statsKey)) :
                new List<FavoriteStat>();
            return instance._database;
        }

        public static void Save(QuickAccessDB database)
        {
            instance._database = database;
            EditorPrefs.SetString(k_statsKey, JsonConvert.SerializeObject(database.Stats));
            instance.Save(true);
        }
    }
}