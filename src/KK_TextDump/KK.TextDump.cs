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
        public const string PluginNameInternal = "KK_TextDump";

        public TextDump()
        {
            TextResourceHelper = new KK_TextResourceHelper();
            AssetDumpHelper = new KK_AssetDumpHelper(this);
        }
    }
}
