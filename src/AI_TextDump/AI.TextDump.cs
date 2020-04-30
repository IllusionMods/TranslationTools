using BepInEx;
using IllusionMods.Shared;
namespace IllusionMods
{
    /// <remarks>
    ///     Uses studio executable for single stage dump.
    /// </remarks>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_TextDump";

        static TextDump()
        {
            CurrentExecutionMode = ExecutionMode.Startup;
        }

        public TextDump()
        {
            TextResourceHelper = new AI_TextResourceHelper();
            AssetDumpHelper = new AI_AssetDumpHelper(this);
        }
    }
}
