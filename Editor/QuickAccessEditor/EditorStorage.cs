using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.QuickAccess
{
    [FilePath("ProjectSettings/UniCore_Editor_QuickAccess.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class EditorStorage : ScriptableSingleton<EditorStorage>
    {
        [SerializeField] private QuickAccessDB _database;

        public static QuickAccessDB Database()
        {
            instance._database ??= new QuickAccessDB();
            return instance._database;
        }

        public static void Save(QuickAccessDB database)
        {
            instance._database = database;
            instance.Save(true);
        }
    }
}