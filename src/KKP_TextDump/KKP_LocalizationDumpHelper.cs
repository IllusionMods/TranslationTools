using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using Localize.Translate;
using Manager;

namespace IllusionMods
{
    public class KKP_LocalizationDumpHelper : LocalizationDumpHelper
    {
        static KKP_LocalizationDumpHelper()
        {
            FormatStringRegex = new Regex(@"(\{[0-9]\}|\[[PH][^\]]*\])");
        }

        public KKP_LocalizationDumpHelper(TextDump plugin) : base(plugin) { }

        protected override IEnumerable<TranslationGenerator> GetLocalizationGenerators()
        {
            foreach (var generator in base.GetLocalizationGenerators())
            {
                yield return generator;
            }

            var readyToDump = TextDump.IsReadyForFinalDump();

            yield return WrapTranslationCollector("Names/ScenarioChars", CollectScenarioCharsLocalizations);
            yield return WrapTranslationCollector("Names/Clubs", CollectClubNameLocalizations);
            yield return WrapTranslationCollector("Names/Personalities", CollectPersonalityLocalizations);
            yield return WrapTranslationCollector("WakeUp", CollectCycleLocalizaitons);
        }


        private Dictionary<string, string> CollectPersonalityLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;
            if (!Singleton<Voice>.IsInstance()) return results;

            var voiceInfos = Singleton<Voice>.Instance.voiceInfoDic ?? new Dictionary<int, VoiceInfo.Param>();

            var assetBundleNames = GetAssetBundleNameListFromPath("etcetra/list/config/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var asset = ManualLoadAsset<VoiceInfo>(assetBundleName, assetName, null);
                    if (asset is null) continue;
                    foreach (var voice in asset.param.Where(voice => !voice.Personality.IsNullOrEmpty()))
                    {
                        var localization = string.Empty;

                        if (voiceInfos.TryGetValue(voice.No, out var voiceParam))
                        {
                            localization = voiceParam.Personality;
                        }


                        AddLocalizationToResults(results, voice.Personality, localization);
                        AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, voice.Personality, localization);
                    }
                }
            }

            return results;
        }

        private Dictionary<string, string> CollectCycleLocalizaitons()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;
            if (!Singleton<Game>.IsInstance()) return results;

            var cycle = Singleton<Game>.Instance?.actScene?.Cycle;

            if (cycle == null) return results;

            var getWords = AccessTools.Method(cycle.GetType(), "GetWords");
            if (getWords == null) return results;

            var lookupTable = new Dictionary<string, string>
            {
                {"Date", "_wakeUpDateWords"},
                {"Holiday", "_wakeUpHolidayWords"},
                {"Saturday", "_wakeUpSaturdayWords"},
                {"WakeUp", "_wakeUpWeekdayWords"}
            };

            foreach (var entry in lookupTable)
            {
                var prop = AccessTools.Field(cycle.GetType(), entry.Value);

                var orig = (string[]) prop?.GetValue(cycle);
                if (orig == null) continue;

                var trans = (string[]) getWords.Invoke(cycle, new object[] {entry.Key});
                for (var i = 0; i < orig.Length; i++)
                {
                    AddLocalizationToResults(results, orig[i], trans != null && trans.Length > i ? trans[i] : string.Empty);
                }
            }

            // stray one in another location
            var wakeUpWordsProp = AccessTools.Field(typeof(ActionGame.Point.ActionPoint), "_wakeUpWords");

            var wakeUpWords = (string[])wakeUpWordsProp?.GetValue(cycle);

            if (wakeUpWords != null)
            {
                var wakeUpTranslations = Singleton<Game>.Instance.actScene.uiTranslater.Get(6).Values.ToArray("Sleep", false);

                for (var i = 0; i < wakeUpWords.Length; i++)
                {
                    AddLocalizationToResults(results, wakeUpWords[i],
                        wakeUpTranslations != null && wakeUpTranslations.Length > i ? wakeUpTranslations[i] : string.Empty);
                }
            }
            return results;
        }

        private Dictionary<string, string> CollectClubNameLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;
            if (!Singleton<Game>.IsInstance()) return results;

            var clubInfos = Game.ClubInfos ?? new Dictionary<int, ClubInfo.Param>();


            var assetBundleNames = GetAssetBundleNameListFromPath("action/list/clubinfo/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var asset = ManualLoadAsset<ClubInfo>(assetBundleName, assetName, null);
                    if (asset is null) continue;
                    foreach (var club in asset.param.Where(club => !club.Name.IsNullOrEmpty()))
                    {
                        var localization = string.Empty;

                        if (clubInfos.TryGetValue(club.ID, out var clubParam))
                        {
                            localization = clubParam.Name;
                        }

                        AddLocalizationToResults(results, club.Name, localization);
                        AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, club.Name, localization);

                    }
                }
            }
            return results;
        }

        private Dictionary<string, string> CollectScenarioCharsLocalizations()
        {
            var results = new Dictionary<string, string>();
            if (!Localize.Translate.Manager.initialized) return results;

            var propInfo = AccessTools.Property(typeof(Localize.Translate.Manager), "ScenarioReplaceNameData");

            var scenarioReplaceNameData =
                propInfo?.GetValue(null, new object[0]) as
                    Dictionary<string, List<ScenarioCharaName.Param>>;


            if (scenarioReplaceNameData == null) return results;

            foreach (var name in scenarioReplaceNameData.Values.SelectMany(nameList => nameList))
            {
                AddLocalizationToResults(results, name.Target, name.Replace);
                if (SpeakerLocalizations != null)
                {
                    AddLocalizationToResults(SpeakerLocalizations, name.Target, name.Replace);
                }

                AddLocalizationToResults(Plugin.TextResourceHelper.GlobalMappings, name.Target, name.Replace);
            }

            return results;
        }
    }
}
