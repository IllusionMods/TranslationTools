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
            if (sender.ScenarioDataHandler is IPathListBoundHandler scenarioHandler)
            {
                scenarioHandler.WhiteListPaths.Add("abdata/adv");
            }

            //if (sender.TextAssetTableHandler is IPathListBoundHandler tableHandler)
            //    tableHandler.WhiteListPaths.Add("abdata/h/list");

            if (sender.ChaListDataHandler is IPathListBoundHandler chaListHandler)
            {
                chaListHandler.WhiteListPaths.Add("abdata/list/characustom");
            }
        }
    }
}
