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
        public const string PluginNameInternal = "KKP_TextDump";

        public TextDump()
        {
            textResourceHelper = new KK_TextResourceHelper();
            assetDumpHelper = new KK_AssetDumpHelper(this);
        }
    }
}
