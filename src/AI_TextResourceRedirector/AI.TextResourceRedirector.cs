using BepInEx;
using System;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "AI_TextResourceRedirector";

        internal TitleSkillNameHandler TitleSkillNameHandler;

        private TextResourceHelper GetTextResourceHelper() => new AI_TextResourceHelper();

        private TextAssetTableHelper GetTextAssetTableHelper() => new TextAssetTableHelper(new string[] { "\r\n", "\r", "\n" }, new string[] { "\t" });

        public TextResourceRedirector()
        {
            TextResourceRedirectorAwake += ConfigureHandlersForAI;
        }

        private void ConfigureHandlersForAI(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender.TitleSkillNameHandler = new TitleSkillNameHandler();
            sender.ChaListDataHandler.WhiteListPaths.Add("abdata/list/characustom");
        }
    }
}
