using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;

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
            Harmony.CreateAndPatchAll(typeof(Hooks));
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

        internal static class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Manager.Resources), nameof(Manager.Resources.LoadMapIK),
                new[] {typeof(AIProject.DefinePack)})]
            internal static void LoadMapIKPrefix(AIProject.DefinePack definePack)
            {
                Logger.LogFatal($"THIS IS IT: {definePack.ABDirectories.MapIKList}");
            }

        }
    }
}
