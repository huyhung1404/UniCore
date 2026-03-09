using UniCore.Utilities;

namespace UniConsole
{
#if UNITY_EDITOR
    [UnityEditor.FilePath("ProjectSettings/UniCore_ConsoleSettings.asset", UnityEditor.FilePathAttribute.Location.ProjectFolder)]
#endif
    public sealed class ConsoleEditorSettings : UniSettingsEditorBase<ConsoleEditorSettings, ConsoleSettings>
    {
    }
}