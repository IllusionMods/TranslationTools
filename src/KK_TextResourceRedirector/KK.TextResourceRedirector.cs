using System;
using BepInEx;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "KK_TextResourceRedirector";

        public TextResourceRedirector()
        {
            TextResourceRedirectorAwake += ConfigureHandlersForKK;
        }

        private TextResourceHelper GetTextResourceHelper()
        {
            return new KK_TextResourceHelper();
        }

        private TextAssetTableHelper GetTextAssetTableHelper()
        {
            return new TextAssetTableHelper(new[] {"\r\n", "\r", "\n"}, new[] {"\t"});
        }

        private void ConfigureHandlersForKK(TextResourceRedirector sender, EventArgs eventArgs)
        {
            // limit what handlers will attempt to handle to speed things up
            sender.ScenarioDataHandler.WhiteListPaths.Add("abdata/adv");
            //sender.ExcelDataHandler.WhiteListPaths.Add("abdata/communication"); // faster without path limiting
            if (sender.TextAssetTableHandler is IPathListBoundHandler pthHandler) pthHandler.WhiteListPaths.Add("abdata/h/list");
            sender.ChaListDataHandler.WhiteListPaths.Add("abdata/list/characustom");
        }
    }
}
