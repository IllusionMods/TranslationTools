using System;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    [BepInPlugin(GUID, PluginName, Version)]
    public partial class TextResourceRedirector
    {
        public const string PluginNameInternal = "KK_TextResourceRedirector";

        public NickNameHandler NickNameHandler { get; private set; }
        public MapInfoHandler MapInfoHandler { get; private set; }
        public EventInfoHandler EventInfoHandler { get; private set; }

        public MakerCustomDataHandler MakerCustomDataHandler { get; private set; }

        public TextResourceRedirector()
        {
            TextResourceRedirectorAwake += ConfigureHandlersForKK;
        }

        private TextResourceHelper GetTextResourceHelper()
        {
            return CreateHelper<KK_TextResourceHelper>();
        }

        private void ConfigureHandlersForKK(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender.NickNameHandler = new NickNameHandler(sender);
            sender.MapInfoHandler = new MapInfoHandler(sender);
            sender.EventInfoHandler = new EventInfoHandler(sender);
            sender.MakerCustomDataHandler = new MakerCustomDataHandler(sender);


            // limit what handlers will attempt to handle to speed things up
            if (sender.ScenarioDataHandler is IPathListBoundHandler scenarioHandler)
            {
                scenarioHandler.WhiteListPaths.Add("abdata/adv");
            }

            //if (sender.TextAssetTableHandler is IPathListBoundHandler tableHandler)
            //    tableHandler.WhiteListPaths.Add("abdata/h/list");

            TextAssetTableHandler.TableRulesGetters.Add(HTextRulesGetter);

            if (sender.ChaListDataHandler is IPathListBoundHandler chaListHandler)
            {
                chaListHandler.WhiteListPaths.Add("abdata/list/characustom");
            }

            /*
            if (sender.TextAssetRawBytesHandler is IPathListBoundHandler textAssetRawBytesHandler)
            {
                TextAssetRawBytesHandler.Enabled = true;
                textAssetRawBytesHandler.WhiteListPaths.Add("abdata/action/fixchara");
            }
            */
        }

        protected bool HTextRulesGetter(string calculatedModificationPath, TextAsset asset, IAssetOrResourceLoadedContext context, out HashSet<int> rowWhitelist, out HashSet<int> rowBlacklist, out HashSet<int> colWhitelist, out HashSet<int> colBlacklist)
        {
            rowWhitelist = null;
            rowBlacklist = null;
            colWhitelist = null;
            colBlacklist = null;

            if (!calculatedModificationPath.Contains(@"h\list")) return false;

            if (!calculatedModificationPath.Contains("personality")) return false;

            colWhitelist = new HashSet<int>(TextResourceHelper.TableHelper.HTextColumns);
            return true;
        }
    }
}
