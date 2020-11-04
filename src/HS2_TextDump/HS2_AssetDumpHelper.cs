using System;
using System.Collections.Generic;
using System.Linq;
using IllusionMods.Shared;
using Manager;
using UnityEngine;

namespace IllusionMods
{
    internal class HS2_AssetDumpHelper : AI_HS2_AssetDumpHelper
    {
        protected HS2_AssetDumpHelper(TextDump plugin) : base(plugin) { }

        public override void InitializeHelper()
        {
            AssetDumpGenerators.Insert(0, GetBgmNameInfoDumpers);
            AssetDumpGenerators.Insert(0, GetEventContentInfoDumpers);
            AssetDumpGenerators.Add(GetParameterNameInfoDumpers);
            AssetDumpGenerators.Add(GetAchievementInfoDumpers);
            AssetDumpGenerators.Add(GetMapInfoDumpers);
            AssetDumpGenerators.Add(GetPlanNameInfoDumpers);

            base.InitializeHelper();
        }


        protected override IEnumerable<string> GetHTypeNames()
        {
            string[] result;
            try
            {
                var field = HSceneManager.HResourceTables.GetType().GetField("assetNames");

                result = field?.GetValue(HSceneManager.HResourceTables) as string[];
            }
            catch
            {
                result = null;
            }

            return result ?? base.GetHTypeNames();
        }

        protected override string GetParamEntryTranslation(object paramEntry)
        {
            if (paramEntry is VoiceInfo.Param viParam) return viParam.EnUS;
            return base.GetParamEntryTranslation(paramEntry);
        }

        protected override string GetMapInfoPath()
        {
            return AssetBundleNames.MapListMapinfoPath ?? base.GetMapInfoPath();
        }



        protected override IEnumerable<ITranslationDumper> GetMapInfoDumpers()
        {
            var bundlePath = GetMapInfoPath();
            if (bundlePath.IsNullOrEmpty()) yield break;

            foreach (var assetBundleName in GetAssetBundleNameListFromPath(bundlePath, true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<MapInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        void AddResult(string[] strings)
                        {
                            if (strings == null || strings.Length < 1) return;
                            AddLocalizationToResults(translations, strings[0],
                                strings.Length > 1 && !string.IsNullOrEmpty(strings[1])
                                    ? strings[1]
                                    : string.Empty);
                        }

                        foreach (var entry in asset.param)
                        {
                            AddResult(entry.MapNames.ToArray());
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        /*
        protected override bool IsValidExcelLocalization(string assetBundleName, string assetName, int firstRow,
            int row, string origString,
            string possibleTranslation)
        {
            // HS2 already has translation columns in table (mostly empty)
            // row 0 is always 'None' which isn't always correct
            if (possibleTranslation == "None" && origString != "なし" && (row == 0 || row == firstRow)) return false;
            return base.IsValidExcelLocalization(assetBundleName, assetName, row, firstRow, origString,
                possibleTranslation);
        }

        protected override bool IsValidChaListDataLocalization(int id, List<string> entry, string origString,
            string possibleTranslation)
        {
            // HS2 already has translation columns in table (mostly empty)
            // row 0 is always 'None' which isn't always correct
            if (possibleTranslation == "None" && origString != "なし" && id == 0) return false;

            return base.IsValidChaListDataLocalization(id, entry, origString, possibleTranslation);
        }
        */

        protected IEnumerable<ITranslationDumper> GetBgmNameInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(AssetBundleNames.GamedataBgmnamePath, true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<BGMNameInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        void AddResult(string[] strings)
                        {
                            if (strings == null || strings.Length < 1) return;
                            AddLocalizationToResults(translations, strings[0],
                                strings.Length > 1 && !string.IsNullOrEmpty(strings[1])
                                    ? strings[1]
                                    : string.Empty);
                        }

                        foreach (var entry in asset.param)
                        {
                            AddResult(entry.name);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        protected IEnumerable<ITranslationDumper> GetEventContentInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(AssetBundleNames.GamedataEventcontentPath,
                true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<EventContentInfoData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        void AddResult(string[] strings)
                        {
                            if (strings == null || strings.Length < 1) return;
                            AddLocalizationToResults(translations, strings[0],
                                strings.Length > 1 && !string.IsNullOrEmpty(strings[1])
                                    ? strings[1]
                                    : string.Empty);
                        }

                        foreach (var entry in asset.param)
                        {
                            AddResult(entry.eventNames);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }


        protected IEnumerable<ITranslationDumper> GetParameterNameInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(
                AssetBundleNames.EtcetraListParameternamePath, true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<ParameterNameInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;


                        void AddResult(string[] strings, string prefix = null)
                        {
                            if (strings == null || strings.Length < 1 || string.IsNullOrEmpty(strings[0])) return;
                            var value = strings.Length > 1 && !string.IsNullOrEmpty(strings[1])
                                ? strings[1]
                                : string.Empty;

                            AddLocalizationToResults(translations, strings[0], value);

                            if (string.IsNullOrEmpty(prefix)) return;

                            AddLocalizationToResults(translations, $"{prefix.ToUpperInvariant()}:{strings[0]}",
                                value);
                        }

                        void AddResults(Func<ParameterNameInfo.Param, string[]> getter, string prefix = null)
                        {
                            foreach (var entry in asset.param)
                            {
                                AddResult(getter(entry), prefix);
                            }
                        }

                        // a bit redundant, but easiest to follow dump order with specialized 
                        // entries grouped at the end
                        AddResults(e => e.trait);
                        AddResults(e => e.mind);
                        AddResults(e => e.state);
                        AddResults(e => e.hattribute);

                        AddResults(e => e.trait, nameof(ParameterNameInfo.Param.trait));
                        AddResults(e => e.mind, nameof(ParameterNameInfo.Param.mind));
                        AddResults(e => e.state, nameof(ParameterNameInfo.Param.state));
                        AddResults(e => e.hattribute, nameof(ParameterNameInfo.Param.hattribute));


                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        protected IEnumerable<ITranslationDumper> GetAchievementInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(AssetBundleNames.GamedataPath, true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<AchievementInfoData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        void AddResult(string[] strings)
                        {
                            if (strings == null || strings.Length < 1) return;
                            AddLocalizationToResults(translations, strings[0],
                                strings.Length > 1 && !string.IsNullOrEmpty(strings[1])
                                    ? strings[1]
                                    : string.Empty);
                        }

                        foreach (var entry in asset.param)
                        {
                            AddResult(entry.title);
                            AddResult(entry.content);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        protected override IEnumerable<ITranslationDumper> GetHTextDumpers()
        {
            foreach (var dumper in base.GetHTextDumpers()) yield return dumper;

            var cellsToDump = TableHelper.HTextColumns;
            if (cellsToDump.Count == 0) yield break;

            foreach (var assetBundleName in GetAssetBundleNameListFromPath("list/h/sound/voice/"))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName).Where(x =>
                    x.StartsWith("HVoice_", StringComparison.OrdinalIgnoreCase) ||
                    x.StartsWith("HVoiceStart_", StringComparison.OrdinalIgnoreCase)))
                {
                    if (!assetName.EndsWith(".txt")) continue;

                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        bool CellHandler(int i, int j, string contents)
                        {
                            if (!cellsToDump.Contains(j)) return false;
                            if (i == 0 && int.TryParse(contents, out _)) return false;
                            AddLocalizationToResults(translations, contents, string.Empty);
                            return true;
                        }

                        TableHelper.ActOnCells(asset, CellHandler, out _);
                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        private IEnumerable<ITranslationDumper> GetPlanNameInfoDumpers()
        {
            var bundlePath = "spr/list/";
            foreach (var assetBundleName in GetAssetBundleNameListFromPath(bundlePath, true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName)
                    .Where(x => x.StartsWith("planname", StringComparison.OrdinalIgnoreCase)))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<PlanNameInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        void AddResult(string[] strings)
                        {
                            if (strings == null || strings.Length < 1) return;
                            AddLocalizationToResults(translations, strings[0],
                                strings.Length > 1 && !string.IsNullOrEmpty(strings[1])
                                    ? strings[1]
                                    : string.Empty);
                        }

                        foreach (var entry in asset.param)
                        {
                            AddResult(entry.name?.ToArray());
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }
    }
}
