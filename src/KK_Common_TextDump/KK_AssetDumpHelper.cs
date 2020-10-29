using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionGame;
using IllusionMods.Shared;
using UnityEngine;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class KK_AssetDumpHelper : AssetDumpHelper
    {
        private static readonly char[] PathSplitter =
            new HashSet<char> {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}.ToArray();

        protected KK_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            AssetDumpGenerators.Add(GetCustomListDumpers);
            AssetDumpGenerators.Add(GetAnimationInfoDumpers);
            AssetDumpGenerators.Add(GetHPointToggleDumpers);
            AssetDumpGenerators.Add(GetSpecialNickNameDumpers);
            AssetDumpGenerators.Add(GetEventInfoDumpers);

            AssetDumpGenerators.Add(GetScenarioTextMergers);
            AssetDumpGenerators.Add(GetCommunicationTextMergers);

            TextDump.TranslationPostProcessors.Add(OptionDisplayItemsPostProcessor);
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
                    if (!assetName.EndsWith(".txt")) continue;

                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        bool CellHandler(int _, int j, string contents)
                        {
                            if (!cellsToDump.Contains(j)) return false;
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

        protected virtual IEnumerable<ITranslationDumper> GetSpecialNickNameDumpers()
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
                        foreach (var entry in asset.param.Where(e => e.isSpecial))
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
            TranslationDumper<IDictionary<string, string>>.TranslationCollector BuildDumper(ExcelData asset,
                string assetName = null)
            {
                if (string.IsNullOrEmpty(assetName)) assetName = asset.name;

                // cache results, only process once
                OrderedDictionary<string, string> results = null;

                IDictionary<string, string> Dumper()
                {
                    if (results != null) return results;

                    results = new OrderedDictionary<string, string>();
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

                            if (asset.name.Contains("_pose") && row.Count >= 7)
                            {
                                colToDump = 3;
                            }
                            else if (row.Count >= 9)
                            {
                                colToDump = 2;
                            }
                            else if (row.Count > 2) colToDump = 1;
                        }
                    }
                    else
                    {
                        var header = ResourceHelper.GetExcelHeaderRow(asset, out firstRow);
                        colToDump = header.IndexOf("デフォルト");
                    }

                    if (colToDump == -1) return results;

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
                    var processor = GetTranslateManagerRowProcessor(3, mapIdx, colToDump);
                    var items = GetExcelEntries(asset, assetName).ToList();
                    for (var i = firstRow; i < items.Count; i++)
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
                            Logger.LogFatal($"GetCustomListDumpers: {err}\n{err.StackTrace}");
                            throw;
                        }
                    }

                    return results;
                }

                return Dumper;
            }


            var assetBundleNames = GetAssetBundleNameListFromPath("custom/", true);
            foreach (var assetBundleName in assetBundleNames)
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName).Where(n => n.StartsWith("cus_")))
                {
                    var asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, null);
                    var dumper = BuildDumper(asset, assetName);
                    foreach (var filePath in BuildCustomListDumperAssetFilePaths(assetBundleName, assetName))
                    {
                        yield return new StringTranslationDumper(filePath, dumper);
                    }
                }

                // some custom lists have multiple assets with the same name in the same bundle, so also get them this way
                var assetBundleData = new AssetBundleData(assetBundleName, null);
                foreach (var asset in assetBundleData.GetAllAssets<ExcelData>())
                {
                    var assetName = asset.name;
                    var dumper = BuildDumper(asset, assetName);
                    foreach (var filePath in BuildCustomListDumperAssetFilePaths(assetBundleName, assetName))
                    {
                        yield return new StringTranslationDumper(filePath, dumper);
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
            var paths = TextDump.GetTranslationPaths().Where(k => k.Contains(needle)).ToList();
            paths.Sort();
            paths.Reverse();

            var splitter = new HashSet<char> {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}.ToArray();

            var mappings = new Dictionary<string, Dictionary<string, string>>();
            var fileMaps = new Dictionary<string, List<string>>();

            foreach (var path in paths)
            {
                var parent = Path.GetFileName(Path.GetDirectoryName(path));
                if (parent is null) continue;
                if (!mappings.TryGetValue(parent, out var personalityMap))
                {
                    mappings[parent] = personalityMap = new Dictionary<string, string>(new TrimmedStringComparer());
                }

                if (!fileMaps.TryGetValue(parent, out var personalityFiles))
                {
                    fileMaps[parent] = personalityFiles = new List<string>();
                }

                personalityFiles.Add(path);

                foreach (var entry in TextDump.GetTranslationsForPath(path)
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
            var paths = TextDump.GetTranslationPaths().Where(k => k.Contains(needle)).ToList();
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

                if (!mappings.TryGetValue(personality, out var personalityMap))
                {
                    mappings[personality] =
                        personalityMap = new Dictionary<string, string>(new TrimmedStringComparer());
                }

                if (!fileMaps.TryGetValue(personality, out var personalityFiles))
                {
                    fileMaps[personality] = personalityFiles = new List<string>();
                }

                personalityFiles.Add(path);

                foreach (var entry in TextDump.GetTranslationsForPath(path)
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

                    var toUpdate = new HashSet<string>(TextDump.GetTranslationsForPath(path)
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
    }
}
