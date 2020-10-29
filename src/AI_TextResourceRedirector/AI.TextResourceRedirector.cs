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
            return CreateHelper<AI_TextResourceHelper>();
        }

        private void ConfigureHandlersForAI(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender.TitleSkillNameHandler = new TitleSkillNameHandler(this, true);
            sender.ChaListDataHandler.WhiteListPaths.Add("abdata/list/characustom");
        }
    }
}
