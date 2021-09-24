using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ActionGame;
using ADV;
using HarmonyLib;
using IllusionMods.Shared;
using IllusionMods.Shared.TextDumpBase;
using KKAPI.Maker.UI;
using UnityEngine;
using static IllusionMods.TextResourceHelper.Helpers;
using Object = UnityEngine.Object;

namespace IllusionMods
{
    public class KKS_AssetDumpHelper : AssetDumpHelper
    {
        private static readonly char[] PathSplitter =
            new HashSet<char> {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}.ToArray();

        protected KKS_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            AssetDumpGenerators.Add(GetMapThumbnailInfoDumpers);
            AssetDumpGenerators.Add(GetAnimationInfoDataDumpers);
            AssetDumpGenerators.Add(GetMonologueInfoDumpers);
            AssetDumpGenerators.Add(GetPrayInfoDumpers);
            AssetDumpGenerators.Add(GetClubInfoDumpers);
            AssetDumpGenerators.Add(GetShopInfoDumpers);
            AssetDumpGenerators.Add(GetWhereLiveDumpers);
            AssetDumpGenerators.Add(GetTopicDumpers);
            AssetDumpGenerators.Add(GetEnvSEDataDumpers);
            AssetDumpGenerators.Add(GetFootSEDataDumpers);
            AssetDumpGenerators.Add(GetVoiceInfoDumpers);
            AssetDumpGenerators.Add(GetCustomListDumpers);
            AssetDumpGenerators.Add(GetAnimationInfoDumpers);
            AssetDumpGenerators.Add(GetHPointToggleDumpers);
            AssetDumpGenerators.Add(GetNickNameDumpers);
            AssetDumpGenerators.Add(GetEventInfoDumpers);
            AssetDumpGenerators.Add(GetResultTopicDataDumpers);
            AssetDumpGenerators.Add(GetEstheticVoiceInfoDumpers);


            AssetDumpGenerators.Add(GetScenarioTextMergers);
            AssetDumpGenerators.Add(GetCommunicationTextMergers);

            BaseTextDumpPlugin.TranslationPostProcessors.Add(OptionDisplayItemsPostProcessor);
        }



        protected override string GetMapInfoPath() => "map/list/mapinfo/";

        
        protected virtual bool TryMapInfoTranslationLookup(MapInfo.Param param, out string result)
        {
            result = null;
            return false;
        }

        protected override IEnumerable<ITranslationDumper> GetMapInfoDumpers()
        {
            var mapInfoPath = GetMapInfoPath();
            if (mapInfoPath.IsNullOrEmpty()) yield break;

            var assetBundleNames = GetAssetBundleNameListFromPath(mapInfoPath, true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> Dumper()
                    {
                        var results = new OrderedDictionary<string, string>();
                        var asset = ManualLoadAsset<MapInfo>(assetBundleName, assetName, null);
                        if (asset == null || asset.param.Count == 0) return results;

                        foreach (var entry in asset.param)
                        {
                            if (!TryMapInfoTranslationLookup(entry, out var value))
                            {
                                value = string.Empty;
                            }

                            AddLocalizationToResults(results, ResourceHelper.GetSpecializedKey(entry, entry.MapName),
                                value);
                            AddLocalizationToResults(results, entry.DisplayName, string.Empty);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(filePath, Dumper);
                }
            }
        }

        private bool OptionDisplayItemsPostProcessor(string path, IDictionary<string, string> translations)
        {
            if (!path.StartsWith(TextDump.AssetsRoot)) return true;
            if (!ResourceHelper.IsOptionDisplayItemPath(path)) return true;
            if (!(translations is OrderedDictionary<string, string>)) return true;
            var prefixedTranslations = new OrderedDictionary<string, string>();
            var standardTranslations = new OrderedDictionary<string, string>();

            foreach (var entry in translations)
            {
                AddLocalizationToResults(entry.Key.StartsWith("OPTION[") ? prefixedTranslations : standardTranslations,
                    entry);
            }

            // nothing to do if there's only one type 
            if (prefixedTranslations.Count == 0 || standardTranslations.Count == 0)
            {
                return true;
            }

            // nothing to do if already sorted
            var origKeys = translations.Keys.ToList();
            var newKeys = new List<string>();
            newKeys.AddRange(prefixedTranslations.Keys);
            newKeys.AddRange(standardTranslations.Keys);
            if (origKeys.SequenceEqual(newKeys))
            {
                return true;
            }

            translations.Clear();

            foreach (var entry in prefixedTranslations) AddLocalizationToResults(translations, entry);
            foreach (var entry in standardTranslations) AddLocalizationToResults(translations, entry);

            return false;
        }

#if false
        // TODO: finish this and see if it works without breaking things
        protected virtual IEnumerable<ITranslationDumper> GetClubInfoDumpers()
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("action/list/clubinfo/", true);
            assetBundleNames.Sort();
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var asset = ManualLoadAsset<ClubInfo>(assetBundleName, assetName, null);
                    foreach (var param in asset.param)
                    {
                        //param.Name;
                    }

                }
            }
        }
#endif

        private IEnumerable<ITranslationDumper> GetEstheticVoiceInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("esthetic/list/voice"))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName)
                    .Where(x => x.StartsWith("voicelist_")))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<EstheticVoiceInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var param in asset.param)
                        {
                            foreach (var voiceAsset in param.voiceAssets)
                            {
                                AddLocalizationToResults(translations, voiceAsset.voice, string.Empty);
                            }
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
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("h/list/"))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName)
                    .Where(x => x.StartsWith("personality_voice_")))
                {

                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<VoiceAllData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var param in asset.param)
                        {
                            foreach (var voiceData in param.data)
                            {
                                foreach (var voiceInfo in voiceData.info)
                                {
                                    AddLocalizationToResults(translations, voiceInfo.word, string.Empty);
                                }
                            }
                        }
                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        private IEnumerable<ITranslationDumper> GetResultTopicDataDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("h/list/"))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName)
                    .Where(x => x.StartsWith("result_topic")))
                {

                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<ResultTopicData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var param in asset.param)
                        {
                            AddLocalizationToResults(translations, param.name, string.Empty);
                        }
                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        public int GetAssetLocalizationIndex(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return -1;
            var parts = Path.GetFileNameWithoutExtension(assetName).Split('_');
            if (parts.Length < 2) return -1;
            return int.TryParse(parts.Last(), out var result) ? result : -1;
        }

        protected virtual Func<List<string>, IEnumerable<KeyValuePair<string, string>>> GetTranslateManagerRowProcessor(
            int sceneId, string assetName, int colToDump,
            Func<List<string>, string> idGetter = null)
        {
            return GetTranslateManagerRowProcessor(sceneId, GetAssetLocalizationIndex(assetName), colToDump, idGetter);
        }

        protected virtual Func<List<string>, IEnumerable<KeyValuePair<string, string>>> GetTranslateManagerRowProcessor(
            int sceneId, int mapIdx, int colToDump, Func<List<string>, string> idGetter = null)
        {
            IEnumerable<KeyValuePair<string, string>> TranslateManagerRowProcessor(List<string> row)
            {
                if (row.Count > colToDump && !string.IsNullOrEmpty(row[colToDump]))
                {
                    yield return new KeyValuePair<string, string>(row[colToDump], string.Empty);
                }
            }

            return TranslateManagerRowProcessor;
        }

        protected virtual bool TryEventInfoTranslationLookup(string assetName, EventInfo.Param param, out string result)
        {
            result = null;
            return false;
        }

        protected IEnumerable<ITranslationDumper> GetEventInfoDumpers()
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("action/list/event/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> Dumper()
                    {
                        var results = new OrderedDictionary<string, string>();
                        var asset = ManualLoadAsset<EventInfo>(assetBundleName, assetName, null);
                        if (asset == null || asset.param.Count == 0) return results;

                        foreach (var entry in asset.param)
                        {
                            if (!TryEventInfoTranslationLookup(asset.name, entry, out var value))
                            {
                                value = string.Empty;
                            }

                            AddLocalizationToResults(results, ResourceHelper.GetSpecializedKey(entry, entry.Name),
                                value);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(filePath, Dumper);
                }
            }
        }



        protected virtual bool TryNickNameTranslationLookup(NickName.Param param, out string result)
        {
            result = null;
            return false;
        }

        protected virtual IEnumerable<ITranslationDumper> GetNickNameDumpers()
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("etcetra/list/nickname/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> Dumper()
                    {
                        var results = new OrderedDictionary<string, string>();
                        var asset = ManualLoadAsset<NickName>(assetBundleName, assetName, null);
                        if (asset == null || asset.param.Count == 0) return results;
                        foreach (var entry in asset.param)
                        {
                            if (!TryNickNameTranslationLookup(entry, out var value))
                            {
                                value = string.Empty;
                            }

                            AddLocalizationToResults(results, ResourceHelper.GetSpecializedKey(entry, entry.Name),
                                value);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(filePath, Dumper);
                }
            }
        }


        protected IEnumerable<ITranslationDumper> GetHPointToggleDumpers()
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("h/list/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName)
                    .Where(n => n.StartsWith("HPointToggle", StringComparison.OrdinalIgnoreCase)))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    // H_POINT_LIST = 6
                    var processor = GetTranslateManagerRowProcessor(6, 1, 1);

                    IDictionary<string, string> Dumper()
                    {
                        var results = new OrderedDictionary<string, string>();
                        var asset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, null);
                        if (asset == null || !TableHelper.IsTable(asset)) return results;

                        foreach (var rowString in TableHelper.SplitTableToRows(asset.text))
                        {
                            var row = TableHelper.SplitRowToCells(rowString).ToList();
                            foreach (var entry in processor(row))
                            {
                                AddLocalizationToResults(results, entry);
                            }
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(filePath, Dumper);
                }
            }
        }



        protected IEnumerable<ITranslationDumper> GetAnimationInfoDumpers()
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("h/list/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName)
                    .Where(n => n.StartsWith("AnimationInfo", StringComparison.OrdinalIgnoreCase)))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    // H_POSTURE = 5
                    var processor = GetTranslateManagerRowProcessor(5, assetName, 0,
                        row => row.Count > 1 ? row[1] : string.Empty);

                    IDictionary<string, string> Dumper()
                    {
                        var results = new OrderedDictionary<string, string>();
                        var asset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, null);
                        if (asset == null || !TableHelper.IsTable(asset)) return results;

                        foreach (var rowString in TableHelper.SplitTableToRows(asset.text))
                        {
                            var row = TableHelper.SplitRowToCells(rowString).ToList();
                            foreach (var entry in processor(row))
                            {
                                AddLocalizationToResults(results, entry);
                            }
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(filePath, Dumper);
                }
            }
        }

        protected IEnumerable<ExcelData.Param> GetExcelEntries(ExcelData asset, string assetName)
        {
            if (!assetName.StartsWith("cus_")) return asset.list;

            var maxCell = asset.MaxCell - 1;
            return asset.Get(new ExcelData.Specify(0, 0),
                new ExcelData.Specify(maxCell, asset.list[maxCell].list.Count - 1));
        }

        protected IEnumerable<ITranslationDumper> GetCustomListDumpers()
        {
            TranslationDumper<IDictionary<string, string>>.TranslationCollector BuildDumper(string assetBundleName, ExcelData asset,
                string assetName = null)
            {

                if (asset == null) return () => new TranslationDictionary();

                if (string.IsNullOrEmpty(assetName)) assetName = asset.name;

                // cache results, only process once
                OrderedDictionary<string, string> results = null;

                IDictionary<string, string> Dumper()
                {
                    if (results != null) return results;

                    results = new OrderedDictionary<string, string>();
                    if (asset == null) return results;
                    var firstRow = 0;
                    var assetPath = BuildAssetFilePath(assetBundleName, assetName);
                    var colsToDump = Plugin.TextResourceHelper.GetSupportedExcelColumns(assetPath, asset, out firstRow).ToList();
                    Logger.LogFatal($"{nameof(GetCustomListDumpers)}.{nameof(Dumper)}: {assetPath}, {asset.name}: {string.Join(", ", colsToDump.Select(i=>i.ToString()))}");

                    
                    if (colsToDump.Count < 1)
                    {
                        foreach (var header in ResourceHelper.GetExcelHeaderRows(asset, out firstRow))
                        {
                            var colToDump = header.IndexOf("デフォルト");
                            if (colToDump < 0) continue;
                            colsToDump.Add(colToDump);
                            break;
                        }
                        
                    }

                    if (colsToDump.Count < 1) return results;

                    var mapIdx = -1;

                    if (asset.name.StartsWith("cus_eb_ptn"))
                    {
                        mapIdx = 0;
                    }
                    else if (asset.name.StartsWith("cus_e_ptn"))
                    {
                        mapIdx = 1;
                    }
                    else if (asset.name.StartsWith("cus_m_ptn"))
                    {
                        mapIdx = 2;
                    }
                    else if (asset.name.StartsWith("cus_eyeslook"))
                    {
                        mapIdx = 3;
                    }
                    else if (asset.name.StartsWith("cus_necklook"))
                    {
                        mapIdx = 4;
                    }
                    else if (asset.name.StartsWith("cus_pose"))
                    {
                        mapIdx = 5;
                    }
                    else if (asset.name.StartsWith("cus_filelist"))
                    {
                        mapIdx = 6;
                    }
                    else if (asset.name.StartsWith("cus_selectlist")) mapIdx = 7;


                    // CUSTOM_LIST2 = 3
                    Logger.LogFatal($"{nameof(GetCustomListDumpers)}: {assetBundleName} {asset.name}: ");
                    var processors = colsToDump.Select(col => GetTranslateManagerRowProcessor(3, mapIdx, col)).ToList();
                    var items = GetExcelEntries(asset, assetName).ToList();
                    for (var i = firstRow; i < items.Count; i++)
                    {
                        try
                        {
                            foreach (var processor in processors)
                            {
                                try
                                {
                                    foreach (var entry in processor(items[i].list))
                                    {
                                        if (entry.Key.Contains("unity3d")) continue;
                                        AddLocalizationToResults(results, entry);
                                    }
                                }
                                catch (Exception err)
                                {
                                    Logger.LogWarning(
                                        $"{nameof(GetCustomListDumpers)}: error processing row {i} with {processor}: {err.Message}");
                                    UnityEngine.Debug.LogException(err);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.LogWarning(
                                $"{nameof(GetCustomListDumpers)}:  error processing row {i}: {err.Message}");
                            UnityEngine.Debug.LogException(err);
                        }
                    }

                    return results;
                }

                return Dumper;
            }


            var assetBundleNames = GetAssetBundleNameListFromPath("custom/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                var assetNames = GetAssetNamesFromBundle(assetBundleName);
                if (!(assetNames is null))
                {
                    foreach (var assetName in assetNames.Where(n => n.StartsWith("cus_")))
                    {

                        var asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, null);
                        if (asset == null) continue;
                        var dumper = BuildDumper(assetBundleName, asset, assetName);
                        foreach (var filePath in BuildCustomListDumperAssetFilePaths(assetBundleName, assetName))
                        {
                            yield return new StringTranslationDumper(filePath, dumper);
                        }
                    }
                }



                // some custom lists have multiple assets with the same name in the same bundle, so also get them this way
                var assetBundleData = new AssetBundleData(assetBundleName, null);
                var allAssets = assetBundleData.GetAllAssets<ExcelData>();
                if (!(allAssets is null))
                {
                    foreach (var asset in allAssets)
                    {
                        if (asset == null) continue;
                        var assetName = asset.name;
                        var dumper = BuildDumper(assetBundleName, asset, assetName);
                        foreach (var filePath in BuildCustomListDumperAssetFilePaths(assetBundleName, assetName))
                        {
                            yield return new StringTranslationDumper(filePath, dumper);
                        }
                    }
                }
            }
        }

        private IEnumerable<string> BuildCustomListDumperAssetFilePaths(string assetBundleName, string assetName)
        {
            while (true)
            {
                yield return BuildAssetFilePath(assetBundleName, assetName);
                if (assetBundleName.Contains("customscenelist") && !assetBundleName.Contains("customscenelist.unity3d"))
                {
                    yield return BuildAssetFilePath(
                        CombinePaths(Path.GetDirectoryName(assetBundleName), "customscenelist.unity3d"), assetName);
                }

                var altName = assetName.Replace("_trial", string.Empty);
                if (assetName == altName) yield break;
                assetName = altName;
            }
        }

#if false
        protected IEnumerable<ITranslationDumper> GetCustomListDumpers()
        {
            var assetBundleNames = GetAssetBundleNameListFromPath("custom/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                Logger.LogFatal($"GetCustomListDumpers: {assetBundleName}");
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName).Where(n => n.StartsWith("cus_")))
                {
                    Logger.LogFatal($"GetCustomListDumpers: {assetBundleName}/{assetName}");

                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> Dumper()
                    {
                        var results = new OrderedDictionary<string, string>();
                        var asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, null);
                        if (asset == null) return results;
                        var firstRow = 0;
                        var colToDump = -1;
                        if (asset.list[0].list.Count == 0 || asset.list[0].list[0].IsNullOrEmpty())
                        {
                            var i = 0;
                            while (i < asset.list.Count && asset.list[i].list.Count == 0) i++;
                            if (i < asset.list.Count)
                            {
                                firstRow = i;
                                var row = asset.GetRow(i);

                                if (asset.name.Contains("_pose") && row.Count >= 7) colToDump = 3;
                                else if (row.Count >= 9) colToDump = 2;
                                else if (row.Count > 2) colToDump = 1;
                            }

                        }
                        else
                        {
                            var header = ResourceHelper.GetExcelHeaderRow(asset, out firstRow);
                            colToDump = header.IndexOf("デフォルト");
                        }

                        Logger.LogFatal($"GetCustomListDumpers: {assetBundleName}/{assetName} colToDump={colToDump}, firstRow={firstRow}");

                        if (colToDump == -1) return results;

                        List<ExcelData.Param> lookup = null;
                        var customBase = Singleton<ChaCustom.CustomBase>.Instance;
                        if (asset.name.StartsWith("cus_eyeslook")) lookup = customBase?.lstEyesLook;
                        else if (asset.name.StartsWith("cus_necklook")) lookup = customBase?.lstNeckLook;
                        else if (asset.name.StartsWith("cus_filelist")) lookup = customBase?.lstFileList;
                        else if (asset.name.StartsWith("cus_selectlist")) lookup = customBase?.lstSelectList;
                        else if (asset.name.StartsWith("cus_eb_ptn")) lookup = customBase?.lstEyebrow;
                        else if (asset.name.StartsWith("cus_e_ptn")) lookup = customBase?.lstEye;
                        else if (asset.name.StartsWith("cus_m_ptn")) lookup = customBase?.lstMouth;
                        else if (asset.name.StartsWith("cus_pose")) lookup = customBase?.lstPose;

                        for (var i = firstRow; i < asset.list.Count; i++)
                        {
                            var row = asset.GetRow(i);
                            if (row.Count <= colToDump) continue;
                            var key = row[colToDump];
                            if (key.Contains("unity3d")) continue;
                            var value = string.Empty;
                            if (lookup != null)
                            {
                                var match = lookup.Where(e => e.list.Count > colToDump && e.list[0] == row[0])
                                    .Select(e => e.list[colToDump]).FirstOrDefault();
   
                               /*
                                    .Where(e => e.list.Count > colToDump &&
                                                check.SequenceEqual(e.list.GetRange(0, colToDump - 1)))
                                    .Select(e => e.list[colToDump]).FirstOrDefault();*/

                                if (!string.IsNullOrEmpty(match) && match != key) value = match;
                            }

                            AddLocalizationToResults(results, key, value);
                        }

                        return results;
                    }

                    yield return new StringTranslationDumper(filePath, Dumper);


                }
            }
        }
#endif
        protected IEnumerable<ITranslationDumper> GetCommunicationTextMergers()
        {
            if (!TextDump.IsReadyForFinalDump()) yield break;
            var needle = CombinePaths("", "abdata", "communication", "");
            var paths = BaseTextDumpPlugin.GetTranslationPaths().Where(k => k.Contains(needle)).ToList();
            paths.Sort();
            paths.Reverse();

            var splitter = new HashSet<char> {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}.ToArray();

            var mappings = new Dictionary<string, Dictionary<string, string>>();
            var fileMaps = new Dictionary<string, List<string>>();

            foreach (var path in paths)
            {
                var parent = Path.GetFileName(Path.GetDirectoryName(path));
                if (parent is null) continue;
                var personalityMap = mappings.GetOrInit(parent,
                    () => new Dictionary<string, string>(new TrimmedStringComparer()));
                var personalityFiles = fileMaps.GetOrInit(parent);

                personalityFiles.Add(path);

                foreach (var entry in BaseTextDumpPlugin.GetTranslationsForPath(path)
                    .Where(entry => !entry.Value.IsNullOrEmpty()))
                {
                    AddLocalizationToResults(personalityMap, entry);
                }
            }

            foreach (var translationDumper in BuildTranslationMergers(fileMaps, mappings))
                yield return translationDumper;
        }

        protected IEnumerable<ITranslationDumper> GetScenarioTextMergers()
        {
            if (!TextDump.IsReadyForFinalDump()) yield break;
            var needle = CombinePaths("", "abdata", "adv", "scenario", "");
            var paths = BaseTextDumpPlugin.GetTranslationPaths().Where(k => k.Contains(needle)).ToList();
            paths.Sort();
            paths.Reverse();
            var personalityCheckChars = "01234567890-".ToCharArray();

            var mappings = new Dictionary<string, Dictionary<string, string>>();
            var fileMaps = new Dictionary<string, List<string>>();

            foreach (var path in paths)
            {
                var parts = path.Split(PathSplitter).ToList();
                var personalityIndex = parts.IndexOf("scenario") + 1;
                if (personalityIndex == 0) continue;
                var personality = parts[personalityIndex];
                var isPersonalityFile = personality.Length > 1 && personality.StartsWith("c") &&
                                        personalityCheckChars.Contains(personality[1]);
                if (!isPersonalityFile) continue;

                var personalityMap = mappings.GetOrInit(personality,
                    () => new Dictionary<string, string>(new TrimmedStringComparer()));

                var personalityFiles = fileMaps.GetOrInit(personality);

                personalityFiles.Add(path);

                foreach (var entry in BaseTextDumpPlugin.GetTranslationsForPath(path)
                    .Where(entry => !entry.Value.IsNullOrEmpty()))
                {
                    AddLocalizationToResults(personalityMap, entry);
                }
            }

            foreach (var translationDumper in BuildTranslationMergers(fileMaps, mappings))
                yield return translationDumper;
        }

        protected IEnumerable<ITranslationDumper> BuildTranslationMergers(Dictionary<string, List<string>> fileMaps,
            Dictionary<string, Dictionary<string, string>> mappings)
        {
            foreach (var personalityFileMap in fileMaps)
            {
                var personality = personalityFileMap.Key;
                var personalityMap = mappings[personality];
                var personalityFiles = personalityFileMap.Value;

                foreach (var path in personalityFiles)
                {
                    var mapPath = path.Substring(TextDump.AssetsRoot.Length).TrimStart(PathSplitter);
                    mapPath = CombinePaths(Path.GetDirectoryName(mapPath), Path.GetFileNameWithoutExtension(mapPath));

                    var toUpdate = new HashSet<string>(BaseTextDumpPlugin.GetTranslationsForPath(path)
                        .Where(e => e.Value.IsNullOrWhiteSpace()).Select(e => e.Key));

                    if (toUpdate.Count == 0) continue;

                    IDictionary<string, string> Dumper()
                    {
                        var result = new OrderedDictionary<string, string>();

                        foreach (var key in toUpdate)
                        {
                            if (personalityMap.TryGetValue(key, out var match))
                            {
                                AddLocalizationToResults(result, key, match);
                            }
                        }

                        return result;
                    }

                    yield return new StringTranslationDumper(mapPath, Dumper);
                }
            }
        }

        // new stuff

        protected IEnumerable<ITranslationDumper> GetEnvSEDataDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/sound/se/env", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<EnvSEData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param.clipDatas)
                        {
                            AddLocalizationToResults(translations, entry.name, string.Empty);
                        }

                        foreach (var entry in asset.param.playListDatas)
                        {
                            foreach (var innerEntry in entry.details)
                            {

                                AddLocalizationToResults(translations, innerEntry.name, string.Empty);
                            }
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }
            }
        }

        protected IEnumerable<ITranslationDumper> GetFootSEDataDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/sound/se/footstep", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<FootSEData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.supplement, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }
            }
        }



        protected IEnumerable<ITranslationDumper> GetWhereLiveDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/wherelive", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<WhereLiveData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Explan, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }
        }


        protected IEnumerable<ITranslationDumper> GetTopicDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/topic", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<Topic>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Name, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }
        }

        private IEnumerable<ITranslationDumper> GetShopInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/shopinfo", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<ShopInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Name, string.Empty);
                            AddLocalizationToResults(translations, entry.Explan, string.Empty);
                            AddLocalizationToResults(translations, entry.NumText, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }   
        }

        private IEnumerable<ITranslationDumper> GetMonologueInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/monologue", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<MonologueInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Text, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }   
        }

        private IEnumerable<ITranslationDumper> GetClubInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/clubinfo", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<ClubInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Name, string.Empty);
                            AddLocalizationToResults(translations, entry.Place, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }   
        }


        private IEnumerable<ITranslationDumper> GetPrayInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("action/list/prayinfo", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<PrayInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Name, string.Empty);
                            AddLocalizationToResults(translations, entry.Explan, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }   
        }


        private IEnumerable<ITranslationDumper> GetAnimationInfoDataDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("h/list", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<AnimationInfoData>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations,entry.nameAnimation,string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }   
        }

        
        private IEnumerable<ITranslationDumper> GetMapThumbnailInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("map/list/mapthumbnailinfo", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<MapThumbnailInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Name, string.Empty);
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                }

            }   
            
        }

        protected override void HandleAdvCommandDump(string assetBundleName, string assetName, ScenarioData.Param param,
            IDictionary<string, string> translations,
            ref Dictionary<string, KeyValuePair<string, string>> choiceDictionary, HashSet<string> allJpText)
        {
            switch (param.Command)
            {
                case Command.SelectionAdd:
                case Command.SelectionInsert:
                case Command.SelectionReplace:
                {
                    Logger.DebugLogDebug(
                        $"{nameof(HandleAdvCommandDump)}: {param.Command}: \"{string.Join("\", \"", param.Args)}\"");

                    foreach (var i in Plugin.TextResourceHelper.GetScenarioCommandTranslationIndexes(param.Command))
                    {
                        var key = ResourceHelper.GetSpecializedKey(param, i);
                        allJpText.Add(key);
                        AddLocalizationToResults(translations, key, string.Empty);

                    }

                    break;
                }
                default:
                {
                    base.HandleAdvCommandDump(assetBundleName, assetName, param, translations, ref choiceDictionary,
                        allJpText);
                    break;
                }
            }
        }


        // Communication is different


        protected override IEnumerable<ITranslationDumper> GetCommunicationTextCollectors(string assetBundleName,
            string assetName, string filePath)
        {
            foreach (var dumper in base.GetCommunicationTextCollectors(assetBundleName, assetName, filePath))
            {
                yield return dumper;
            }

            if (assetName.Contains("CommunicationNPC", StringComparison.OrdinalIgnoreCase))
            {
                yield return new StringTranslationDumper(filePath,
                    MakeCommunicationNPCTextCollector(assetBundleName, assetName));
            }
            else if (assetName.Contains("TopicListen", StringComparison.OrdinalIgnoreCase))
            {
                yield return new StringTranslationDumper(filePath,
                    MakeTopicListenTextCollector(assetBundleName, assetName));
            }
            else if (assetName.Contains("TopicPersonalityGroup", StringComparison.OrdinalIgnoreCase))
            {
                yield return new StringTranslationDumper(filePath,
                    MakeTopicPersonalityGroupCollector(assetBundleName, assetName));
            }
            else if (assetName.Contains("TopicTalkCommon", StringComparison.OrdinalIgnoreCase))
            {
                yield return new StringTranslationDumper(filePath,
                    MakeTopicTalkCommonCollector(assetBundleName, assetName));
            }
            else if (assetName.Contains("TopicTalkRare", StringComparison.OrdinalIgnoreCase))

            {
                yield return new StringTranslationDumper(filePath,
                    MakeTopicTalkRareCollector(assetBundleName, assetName));
            } 
            else if (assetName.Contains("TopicTalkRare", StringComparison.OrdinalIgnoreCase))

            {
                yield return new StringTranslationDumper(filePath,
                    MakeTopicTalkRareCollector(assetBundleName, assetName));
            } 
            else if (assetName.StartsWith("tips_", StringComparison.OrdinalIgnoreCase))
            {
                yield return new StringTranslationDumper(filePath,
                    MakeTipsCollector(assetBundleName, assetName));
            }



        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeTipsCollector(string assetBundleName, string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<TipsData>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {
                    AddLocalizationToResults(translations, param.title, string.Empty);
                    AddLocalizationToResults(translations, param.text, string.Empty);
                }

                return translations;
            }

            return AssetDumper;


        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeTopicTalkRareCollector(string assetBundleName, string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<TopicTalkRare>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {
                    AddLocalizationToResults(translations, param.text, string.Empty);
                }

                return translations;
            }

            return AssetDumper;
        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeTopicTalkCommonCollector(
            string assetBundleName, string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<TopicTalkCommon>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {

                    AddLocalizationToResults(translations, param.text, string.Empty);

                }

                return translations;
            }

            return AssetDumper;
        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeTopicPersonalityGroupCollector(string assetBundleName, string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<TopicPersonalityGroup>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {
                    foreach (var personality in param.personality)
                    {
                        AddLocalizationToResults(translations, personality, string.Empty);
                    }
                }

                return translations;
            }

            return AssetDumper;
        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeTopicListenTextCollector(string assetBundleName, string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<TopicListenData>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {
                    AddLocalizationToResults(translations, param.text, string.Empty);
                }

                return translations;
            }

            return AssetDumper;
        }

        private TranslationDumper<IDictionary<string, string>>.TranslationCollector MakeCommunicationNPCTextCollector(string assetBundleName, string assetName)
        { 
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<CommunicationNPCData>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {
                    AddLocalizationToResults(translations, param.text, string.Empty);
                }

                return translations;
            }

            return AssetDumper;
        }

        protected override TranslationDumper<IDictionary<string, string>>.TranslationCollector
            MakeStandardCommunicationTextCollector(string assetBundleName,
                string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<CommunicationInfo>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.param)
                {
                    AddLocalizationToResults(translations, param.text, string.Empty);
                }

                return translations;
            }

            return AssetDumper;
        }
       

        // AI/HS2 similar

        
        protected virtual string GetParamEntryTranslation(object paramEntry)
        {
            return string.Empty;
        }

        protected IEnumerable<ITranslationDumper> GetVoiceInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("etcetra/list/config/", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<VoiceInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Personality, GetParamEntryTranslation(entry));
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }
    }
}
