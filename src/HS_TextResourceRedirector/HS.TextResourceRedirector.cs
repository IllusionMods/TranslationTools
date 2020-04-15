using BepInEx;

namespace IllusionMods
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginNameInternal = "HS_TextResourceRedirector";

        private TextResourceHelper GetTextResourceHelper() => null;
        private TextAssetTableHelper GetTextAssetTableHelper() => null;
    }
}
