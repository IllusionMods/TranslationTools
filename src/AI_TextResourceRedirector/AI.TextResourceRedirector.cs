using System;
using BepInEx;
using HarmonyLib;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "AI_TextResourceRedirector";

        // ReSharper disable once NotAccessedField.Global
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

            var excelSkipPaths = new[]
            {
                "abdata/list/map/area",
                "abdata/list/map/chunk",
                "abdata/list/map/enviro",
                "abdata/list/map/event_item",
                "abdata/list/map/ikinfo",
                "abdata/list/map/mapinfo",
                "abdata/list/map/minimap",
                "abdata/list/map/openstate",
                "abdata/list/map/particle",
                "abdata/list/map/plant_item",
                "abdata/list/map/storypoint",
                "abdata/list/map/timeinfo",
                "abdata/list/map/vanish"
            };

            foreach (var path in excelSkipPaths) sender.ExcelDataHandler.BlackListPaths.Add(path);
        }
    }
}
