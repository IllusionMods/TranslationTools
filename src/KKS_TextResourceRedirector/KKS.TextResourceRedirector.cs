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
        public const string PluginNameInternal = "KKS_TextResourceRedirector";

        public TextResourceRedirector()
        {
            TextResourceRedirectorAwake += ConfigureHandlersForKKS;
        }

        public NickNameHandler NickNameHandler { get; private set; }

        public MapInfoHandler MapInfoHandler { get; private set; }

        public EventInfoHandler EventInfoHandler { get; private set; }

        public MakerCustomDataHandler MakerCustomDataHandler { get; private set; }

        public VoiceInfoHandler VoiceInfoHandler { get; private set; }

        public AnimationInfoDataHandler AnimationInfoDataHandler { get; private set; }

        public ClubInfoHandler ClubInfoHandler { get; private set; }

        public EnvSEDataHandler EnvSEDataHandler { get; private set; }

        public FootSEDataHandler FootSEDataHandler { get; private set; }

        public MonologueInfoHandler MonologueInfoHandler { get; private set; }

        public PrayInfoHandler PrayInfoHandler { get; private set; }

        public TopicHandler TopicHandler { get; private set; }

        public WhereLiveDataHandler WhereLiveDataHandler { get; private set; }

        public CommunicationInfoHandler CommunicationInfoHandler { get; private set; }

        public CommunicationNPCHandler CommunicationNPCHandler { get; private set; }

        public MapThumbnailInfoHandler MapThumbnailInfoHandler { get; private set; }

        public TopicListenDataHandler TopicListenDataHandler { get; private set; }

        public TopicPersonalityGroupHandler TopicPersonalityGroupHandler { get; private set; }

        public TopicTalkCommonHandler TopicTalkCommonHandler { get; private set; }

        public TopicTalkRareHandler TopicTalkRareHandler { get; private set; }

        public VoiceAllDataHandler VoiceAllDataHandler { get; private set; }

        public ResultTopicDataHandler ResultTopicDataHandler { get; private set; }

        public ShopInfoHandler ShopInfoHandler { get; private set; }

        public TipsDataHandler TipsDataHandler { get; private set; }

        public EstheticVoiceInfoHandler EstheticVoiceInfoHandler { get; private set; }

        private TextResourceHelper GetTextResourceHelper()
        {
            return CreateHelper<KKS_TextResourceHelper>();
        }

        private void ConfigureHandlersForKKS(TextResourceRedirector sender, EventArgs eventArgs)
        {
            sender.NickNameHandler = new NickNameHandler(sender);
            sender.MapInfoHandler = new MapInfoHandler(sender);
            sender.EventInfoHandler = new EventInfoHandler(sender);
            sender.MakerCustomDataHandler = new MakerCustomDataHandler(sender);

            sender.ShopInfoHandler = new ShopInfoHandler(sender);

            sender.VoiceInfoHandler = new VoiceInfoHandler(sender);

            sender.AnimationInfoDataHandler = new AnimationInfoDataHandler(sender);
            sender.ClubInfoHandler = new ClubInfoHandler(sender);
            sender.EnvSEDataHandler = new EnvSEDataHandler(sender);
            sender.FootSEDataHandler = new FootSEDataHandler(sender);
            sender.MonologueInfoHandler = new MonologueInfoHandler(sender);
            sender.PrayInfoHandler = new PrayInfoHandler(sender);
            sender.TopicHandler = new TopicHandler(sender);
            sender.WhereLiveDataHandler = new WhereLiveDataHandler(sender);
            sender.CommunicationInfoHandler = new CommunicationInfoHandler(sender);
            sender.CommunicationNPCHandler = new CommunicationNPCHandler(sender);
            sender.MapThumbnailInfoHandler = new MapThumbnailInfoHandler(sender);
            sender.TopicListenDataHandler = new TopicListenDataHandler(sender);
            sender.TopicPersonalityGroupHandler = new TopicPersonalityGroupHandler(sender);
            sender.TopicTalkCommonHandler = new TopicTalkCommonHandler(sender);
            sender.TopicTalkRareHandler = new TopicTalkRareHandler(sender);
            sender.VoiceAllDataHandler = new VoiceAllDataHandler(sender);
            sender.ResultTopicDataHandler = new ResultTopicDataHandler(sender);
            sender.TipsDataHandler = new TipsDataHandler(sender);
            sender.EstheticVoiceInfoHandler = new EstheticVoiceInfoHandler(sender);


            foreach (var handler in new IRedirectorHandler[]
            {
                sender.CommunicationInfoHandler, sender.CommunicationNPCHandler, sender.TopicHandler,
                sender.TopicListenDataHandler, sender.TopicPersonalityGroupHandler, sender.TopicTalkCommonHandler,
                sender.TopicTalkRareHandler, sender.TipsDataHandler
            })
            {
                if (handler is IPathListBoundHandler communicationHandler)
                {
                    communicationHandler.WhiteListPaths.Add("abdata/communication");
                }
            }

            if (sender.VoiceAllDataHandler is IPathListBoundHandler voiceAllDataHandler)
            {
                voiceAllDataHandler.WhiteListPaths.Add("abdata/h/list");
            }

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

            if (sender.EstheticVoiceInfoHandler is IPathListBoundHandler estheticVoiceInfoHandler)
            {
                estheticVoiceInfoHandler.WhiteListPaths.Add("esthetic/list/voice");
            }

            Harmony.CreateAndPatchAll(typeof(Hooks));

            TutorialScopeHelper.Init(this);
        }


        protected bool HTextRulesGetter(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context, out HashSet<int> rowWhitelist, out HashSet<int> rowBlacklist,
            out HashSet<int> colWhitelist, out HashSet<int> colBlacklist)
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

        public static class Hooks
        {
#if false
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Manager.GameAssist), "GetTopic")]
            private static void GetTopicPrefix(GameAssist __instance, Heroine _heroine)
            {
                Logger.LogFatal($"GameAssist.GetTopic: {_heroine}, {_heroine.personality}");
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionGame.Communication.Info),
                nameof(ActionGame.Communication.Info.GetTopicListenInfo))]
            private static void GetTopicListenInfoPrefix(ActionGame.Communication.Info __instance, int _no)
            {
                Logger.LogFatal($"ActionGame.Communication.Info.GetTopicListenInfo: {_no}");
                
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionGame.Communication.Info),
                nameof(ActionGame.Communication.Info.LoadTopicListenData))]
            private static void LoadTopicListenDataPrefix(int _personality)
            {
                Logger.LogFatal($"{nameof(LoadTopicListenDataPrefix)}: personality={_personality}");
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(ActionGame.Communication.Info),
                nameof(ActionGame.Communication.Info.LoadTopicListenData))]
            private static void LoadTopicListenDataPostfix(ActionGame.Communication.Info __instance, int _personality, bool __result)
            {
                Logger.LogFatal($"{nameof(LoadTopicListenDataPostfix)}: personality={_personality}, result={__result}");
                foreach (var entry in __instance.dicTopicListenInfo)
                {
                    Logger.LogFatal($"{nameof(LoadTopicListenDataPostfix)}:{entry.Key} => {entry.Value}");
                }

            }
#endif
        }
    }
}
