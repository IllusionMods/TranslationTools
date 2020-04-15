using BepInEx;
using System;

namespace IllusionMods
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier, XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Version)]
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public const string PluginNameInternal = "AI_TextResourceRedirector";

        internal TitleSkillNameHandler _titleSkillNameHandler;

        private TextResourceHelper GetTextResourceHelper() => new AI_TextResourceHelper();

        private TextAssetTableHelper GetTextAssetTableHelper() => new TextAssetTableHelper(new string[] { "\r\n", "\r", "\n" }, new string[] { "\t" });

        public TextResourceRedirector()
        {
            TextResourceRedirectorAwake += AddTitleSkillNameHandler;
        }

        private void AddTitleSkillNameHandler(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender._titleSkillNameHandler = new TitleSkillNameHandler();
        }
    }
}
