using BepInEx;

namespace IllusionMods
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier, XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginNameInternal = "KK_TextResourceRedirector";

        private TextResourceHelper GetTextResourceHelper() => new KK_TextResourceHelper();
        private TextAssetTableHelper GetTextAssetTableHelper() => new TextAssetTableHelper(new string[] { "\r\n", "\r", "\n" }, new string[] { "\t" });
    }
}
