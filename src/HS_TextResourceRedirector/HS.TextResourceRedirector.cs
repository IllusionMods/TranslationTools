using BepInEx;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "HS_TextResourceRedirector";

        private TextResourceHelper GetTextResourceHelper() => null;


    }
}
