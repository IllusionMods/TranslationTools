using BepInEx;
using IllusionMods.Shared;

namespace IllusionMods
{
    /// <remarks>
    ///     Uses studio executable for single stage dump.
    /// </remarks>
    [BepInProcess(Constants.StudioProcessName)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextDump 
    {
        public const string PluginNameInternal = "KK_TextDump";

        public TextDump()
        {
            SetTextResourceHelper(CreateHelper<KK_TextResourceHelper>());
            AssetDumpHelper = CreatePluginHelper<KK_AssetDumpHelper>();
        }
    }
}
