using UniConsole;

namespace UniCore.Editor.Console
{
    public class ConsoleBuildProcessor : BuildProcessor<ConsoleRuntimeSettings, ConsoleSettings, ConsoleEditorSettings>
    {
        public override string FileName() => ConsoleRuntimeSettings.k_FileName;
    }
}