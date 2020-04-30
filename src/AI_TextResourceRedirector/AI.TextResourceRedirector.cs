using System;
using BepInEx;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "AI_TextResourceRedirector";

        internal TitleSkillNameHandler TitleSkillNameHandler;

        public TextResourceRedirector()
        {
            TextResourceRedirectorAwake += ConfigureHandlersForAI;
        }

        private TextResourceHelper GetTextResourceHelper()
        {
            return new AI_TextResourceHelper();
        }

        private TextAssetTableHelper GetTextAssetTableHelper()
        {
            return new TextAssetTableHelper(new[] {"\r\n", "\r", "\n"}, new[] {"\t"});
        }

        private void ConfigureHandlersForAI(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender.TitleSkillNameHandler = new TitleSkillNameHandler();
            sender.ChaListDataHandler.WhiteListPaths.Add("abdata/list/characustom");
        }
    }
}
