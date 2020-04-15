using BepInEx;

namespace IllusionMods
{
    /// <summary>
    /// Dumps untranslated text to .txt files
    /// </summary>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_TextDump";

        static TextDump()
        {
            WriteOnDump = true;
            CurrentExecutionMode = TextDump.ExecutionMode.Startup;
        }

        public TextDump()
        {
            textResourceHelper = new AI_TextResourceHelper();
        }
    }
}
