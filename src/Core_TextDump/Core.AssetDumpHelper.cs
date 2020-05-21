using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ADV;
using IllusionMods.Shared;
using MessagePack;
using UnityEngine;
using UnityEngine.Assertions;
using static IllusionMods.TextResourceHelper.Helpers;
#if AI
using AIChara;

#endif

namespace IllusionMods
{
    public class AssetDumpHelper : BaseDumpHelper
    {
        public delegate bool TryDumpListEntry(string assetBundleName, string assetName,
            AssetDumpColumnInfo assetDumpColumnInfo, IDictionary<string, string> translations);

        protected readonly List<TranslationGenerator> AssetDumpGenerators;
        protected readonly List<TranslationGenerator> RawAssetDumpGenerators;

        protected AssetDumpColumnInfo StdExcelAssetCols;
        protected AssetDumpColumnInfo StdStudioAssetCols;

        protected AssetDumpColumnInfo StdTextAssetCols;

        public AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            StdTextAssetCols = new AssetDumpColumnInfo(new Dictionary<string, string>
            {
                {"Name", "EN_US"}
            });

            StdStudioAssetCols = new AssetDumpColumnInfo(null, null, true, new[]
            {
                "名称",
                "表示名"
            });

            AssetDumpGenerators = new List<TranslationGenerator>
            {
                GetCommunicationTextDumpers,
                GetScenarioTextDumpers,
                GetHTextDumpers,
                GetListTextDumpers
            };

            //RawAssetDumpGenerators = new List<RawTranslationGenerator> { };

            StdExcelAssetCols = new AssetDumpColumnInfo(new Dictionary<string, string>
            {
                {"Name", "EN_US"},
                {"Text", "Text _EnUS"},
                {"タイトル(日本語)", "タイトル(英語)"},
                {"サブタイトル(日本語)", "サブタイトル(英語)"},
                {"タイトル", "英語"},
                {"日本語", "英語"},
                {"本文(日本語)", "本文(英語？)"},
                {"日本語(0)", "英語"},
                {"選択肢１", "選択肢１(英語)"},
                {"選択肢２", "選択肢２(英語)"},
                {"選択肢３", "選択肢３(英語)"},
                {"名前(メモ)", string.Empty}
            });

            ListEntryDumpers = new List<TryDumpListEntry>
            {
                TryDumpExcelData,
                TryDumpTextAsset
            };
        }

        protected List<TryDumpListEntry> ListEntryDumpers { get; }

        protected LocalizationDumpHelper LocalizationDumpHelper => Plugin?.LocalizationDumpHelper;

        protected virtual string BuildAssetFilePath(string assetBundleName, string assetName,
            string fileName = "translation")
        {
            return CombinePaths(
                Path.GetDirectoryName(assetBundleName),
                Path.GetFileNameWithoutExtension(assetBundleName),
                Path.GetFileNameWithoutExtension(assetName),
                fileName);
        }

        public virtual IEnumerable<ITranslationDumper> GetAssetDumpers()
        {
            Assert.IsNotNull(AssetDumpGenerators);
            var dumpers = AssetDumpGenerators.ToList();

            while (dumpers.Count > 0)
            {
                var assetDumpGenerator = dumpers.PopFront();
                var entries = new List<ITranslationDumper>();
                try
                {
                    foreach (var entry in assetDumpGenerator()) entries.Add(entry);
                }
                catch (Exception err)
                {
                    Logger.LogError($"Error dumping: {assetDumpGenerator} : {err}");
                }

                foreach (var entry in entries) yield return entry;
            }
        }


        /*
        public virtual IEnumerable<ITranslationDumper> GetRawAssetDumpers()
        {
            Assert.IsNotNull(RawAssetDumpGenerators);
            var dumpers = RawAssetDumpGenerators.ToList();

        }
        */

        #region HText

        protected virtual IEnumerable<ITranslationDumper> GetHTextDumpers()
        {
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

        #endregion HText

        public override void PrepareLineForDump(ref string key, ref string value)
        {
            base.PrepareLineForDump(ref key, ref value);
            value = value.Replace(";", ",");
        }

        #region CommunicationText

        protected virtual TranslationDumper<IDictionary<string, string>>.TranslationCollector
            MakeOptionDisplayItemsCollector(string assetBundleName, string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                var mappings = new Dictionary<int, int>();
                var headers = ResourceHelper.GetExcelHeaderRow(asset, out var firstRow);
                for (var i = 1; i < headers.Count; i++)
                {
                    var header = headers[i];
                    if (header.Contains("(") && header.EndsWith(")")) break;
                    mappings.Add(i, headers.IndexOf($"{header}(英語)"));
                }

                for (var rowIndex = firstRow; rowIndex < asset.list.Count; rowIndex++)
                {
                    var row = asset.GetRow(rowIndex);
                    var value = string.Empty;
                    if (row[0] == "no") continue;

                    foreach (var mapping in mappings)
                    {
                        var i = mapping.Key;
                        if (row.Count <= i) continue;
                        if (row[i].IsNullOrWhiteSpace()) continue;
                        if (row[i] == "・・・") continue;

                        if (mapping.Value > -1 && row.Count > mapping.Value)
                        {
                            try
                            {
                                value = row[mapping.Value];
                            }
                            catch
                            {
                                value = string.Empty;
                            }
                        }

                        foreach (var key in ResourceHelper.GetExcelRowTranslationKeys(asset.name, row, i))
                        {
                            AddLocalizationToResults(translations, key, value);
                        }
                    }
                }

                return translations;
            }

            return AssetDumper;
        }

        protected virtual TranslationDumper<IDictionary<string, string>>.TranslationCollector
            MakeStandardCommunicationTextCollector(string assetBundleName,
                string assetName)
        {
            IDictionary<string, string> AssetDumper()
            {
                var translations = new OrderedDictionary<string, string>();
                var asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, "abdata");
                if (asset is null) return translations;

                foreach (var param in asset.list)
                {
                    if (15 > param.list.Count || param.list[15].IsNullOrEmpty() || param.list[15] == "テキスト") continue;

                    var key = param.list[15];
                    var value = param.list.Count > 20 ? param.list[20] : string.Empty;

                    AddLocalizationToResults(translations, key, value);
                }

                return translations;
            }

            return AssetDumper;
        }

        protected virtual IEnumerable<ITranslationDumper> GetCommunicationTextDumpers()
        {
            foreach (var assetBundleName in CommonLib.GetAssetBundleNameListFromPath("communication"))
            {
                if (assetBundleName.Contains("hit_")) continue;

                var assetNames = GetAssetNamesFromBundle(assetBundleName);

                foreach (var assetName in assetNames)
                {
                    if (assetName.Contains("speed_")) continue;

                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    if (ResourceHelper.IsOptionDisplayItemAsset(assetName))
                    {
                        yield return new StringTranslationDumper(filePath,
                            MakeOptionDisplayItemsCollector(assetBundleName, assetName));
                    }
                    else
                    {
                        yield return new StringTranslationDumper(filePath,
                            MakeStandardCommunicationTextCollector(assetBundleName, assetName));
                    }
                }

                if (!string.IsNullOrEmpty(assetBundleName))
                {
                    yield return new StringTranslationDumper($"_{nameof(GetCommunicationTextDumpers)}Cleanup", () =>
                    {
                        AssetBundleManager.UnloadAssetBundle(assetBundleName, false);
                        return new Dictionary<string, string>();
                    });
                }
            }
        }

        #endregion CommunicationText

        #region ScenarioText

        private string BuildReplacementKey(string assetBundleName, string key)
        {
            return JoinStrings("|", Path.GetDirectoryName(assetBundleName), key);
        }

        private void UpdatedReplacementDictionary(string assetBundleName,
            ref Dictionary<string, KeyValuePair<string, string>> replacementDict,
            IEnumerable<ScenarioData.Param> assetParams)
        {
            UpdatedReplacementDictionary(assetBundleName, ref replacementDict, assetParams.ToArray());
        }

        private void UpdatedReplacementDictionary(string assetBundleName,
            ref Dictionary<string, KeyValuePair<string, string>> replacementDict,
            params ScenarioData.Param[] assetParams)
        {
            foreach (var entry in ResourceHelper.BuildReplacements(assetParams))
            {
                replacementDict[BuildReplacementKey(assetBundleName, entry.Key)] = entry.Value;
            }
        }

        private Dictionary<string, KeyValuePair<string, string>> BuildReplacementDictionary(string scenarioRoot)
        {
            var result = new Dictionary<string, KeyValuePair<string, string>>();

            var assetBundleNames = GetAssetBundleNameListFromPath(scenarioRoot, true);
            assetBundleNames.Sort();

            foreach (var assetBundleName in assetBundleNames)
            {
                var assetNameList = new List<string>(GetAssetNamesFromBundle(assetBundleName));
                assetNameList.Sort();
                foreach (var assetName in assetNameList)
                {
                    var asset = ManualLoadAsset<ScenarioData>(assetBundleName, assetName, "abdata");
                    if (asset is null) continue;
                    UpdatedReplacementDictionary(assetBundleName, ref result, asset.list);
                }

                //AssetBundleManager.UnloadAssetBundle(assetBundleName, false);
            }

            return result;
        }

        protected virtual IEnumerable<ITranslationDumper> GetScenarioTextDumpers()
        {
            var allJpText = new HashSet<string>();
            foreach (var scenarioRoot in ResourceHelper.GetScenarioDirs())
            {
                var baseChoiceDictionary = BuildReplacementDictionary(scenarioRoot);
                foreach (var assetBundleName in GetAssetBundleNameListFromPath(scenarioRoot, true))
                {
                    foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                    {
                        var asset = ManualLoadAsset<ScenarioData>(assetBundleName, assetName, "abdata");
                        if (asset?.list is null) continue;

                        var filePath = BuildAssetFilePath(assetBundleName, assetName);
                        var choiceDictionary =
                            new Dictionary<string, KeyValuePair<string, string>>(baseChoiceDictionary);

                        IDictionary<string, string> AssetDumper()
                        {
                            var translations = new OrderedDictionary<string, string>();
                            foreach (var param in asset.list)
                            {
                                // update as we go so most recent entries are in place
                                UpdatedReplacementDictionary(assetBundleName, ref choiceDictionary, param);
                                if (!ResourceHelper.IsSupportedCommand(param.Command))
                                {
#if DEBUG
                                    Logger.LogDebug($"DumpScenarioText: Unsupported: {param.Command}: \"{string.Join("\", \"", param.Args)}\"");
#endif
                                    continue;
                                }


                                switch (param.Command)
                                {
                                    case Command.Text:
                                    case (Command) 242:
                                    {
                                        // Text: 0 - jp speaker (if present), 1 - jp text,  2 - eng text
                                        if (param.Args.Length == 0) continue;

                                        var speaker = param.Args[0];
                                        if (!speaker.IsNullOrEmpty() && !StringIsSingleReplacement(speaker) &&
                                            !ResourceHelper.TextKeysBlacklist.Contains(speaker))
                                        {
                                            // capture speaker name
                                            AddLocalizationToResults(translations, speaker,
                                                LookupSpeakerLocalization(speaker, assetBundleName, assetName));
                                        }

                                        if (param.Args.Length >= 2 && !param.Args[1].IsNullOrEmpty())
                                        {
                                            var key = param.Args[1];
                                            var value = string.Empty;
                                            if (!ResourceHelper.TextKeysBlacklist.Contains(key))
                                            {
                                                if (param.Args.Length >= 3 && !param.Args[2].IsNullOrEmpty())
                                                {
                                                    value = param.Args[2];
                                                }

                                                allJpText.Add(key);
                                                AddLocalizationToResults(translations, key, value);
                                            }
                                        }

                                        break;
                                    }
                                    case Command.Calc:
                                    {
                                        //Logger.LogDebug($"DumpScenarioText: {param.Command}: \"{string.Join("\", \"", param.Args)}\"");
                                        if (param.Args.Length >= 3 && ResourceHelper.CalcKeys.Contains(param.Args[0]))
                                        {
                                            var key = ResourceHelper.GetSpecializedKey(param, 2, out var value);
                                            allJpText.Add(key);
                                            AddLocalizationToResults(translations, key, value);
                                        }

                                        break;
                                    }
                                    case Command.Format:
                                    {
                                        //Logger.LogDebug($"DumpScenarioText: {param.Command}: \"{string.Join("\", \"", param.Args)}\"");
                                        if (param.Args.Length >= 2 && ResourceHelper.FormatKeys.Contains(param.Args[0]))
                                        {
                                            var key = param.Args[1];
                                            allJpText.Add(key);
                                            AddLocalizationToResults(translations, key, string.Empty);

                                            // not sure where localizations are, but they're not in the next arg
                                        }

                                        break;
                                    }
                                    case Command.Choice:
                                    {
                                        for (var i = 0; i < param.Args.Length; i++)
                                        {
                                            var key = ResourceHelper.GetSpecializedKey(param, i, out var fallbackValue);
                                            if (key.IsNullOrEmpty()) continue;
                                            var value = string.Empty;
                                            if (choiceDictionary.TryGetValue(
                                                BuildReplacementKey(assetBundleName,
                                                    fallbackValue.TrimStart('[').TrimEnd(']')), out var entry))
                                            {
                                                key = ResourceHelper.BuildSpecializedKey(param, entry.Key);
                                                value = entry.Value;
                                            }

                                            allJpText.Add(key);
                                            AddLocalizationToResults(translations, key, value);
                                        }

                                        break;
                                    }
                                    case Command.Switch:
                                    {
                                        for (var i = 0; i < param.Args.Length; i++)
                                        {
                                            var key = ResourceHelper.GetSpecializedKey(param, i, out var value);
                                            allJpText.Add(key);
                                            AddLocalizationToResults(translations, key, value);
                                        }

                                        break;
                                    }
#if AI
                                    case Command.InfoText:
                                    {
                                        for (var i = 2; i < param.Args.Length; i += 2)
                                        {
                                            var key = param.Args[i];
                                            allJpText.Add(key);
                                            AddLocalizationToResults(translations, key, string.Empty);
                                        }

                                        break;
                                    }
#endif
                                    case Command.Jump:
                                    {
                                        if (param.Args.Length >= 1 &&
                                            ContainsNonAscii(param.Args[0]))
                                        {
                                            allJpText.Add(param.Args[0]);
                                            AddLocalizationToResults(translations, param.Args[0],
                                                "Jump");
                                        }

                                        break;
                                    }
                                    default:
                                        Logger.LogError(
                                            $"[TextDump] Unhandled command: {param.Command}: '{string.Join("', '", param.Args.Select(a => a?.ToString() ?? string.Empty).ToArray())}'");
                                        break;
                                }
                            }

                            return translations;
                        }

                        yield return new StringTranslationDumper(filePath, AssetDumper);
                    }
                }
            }
        }

        protected virtual string LookupSpeakerLocalization(string speaker, string bundle, string asset)
        {
            return ResourceHelper.GlobalMappings.TryGetValue(speaker, out var result) ? result : string.Empty;
        }

        #endregion ScenarioText

        #region Lists

        protected virtual IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            yield return new KeyValuePair<string, AssetDumpColumnInfo>("characustom", StdTextAssetCols);
        }

        protected virtual IEnumerable<ITranslationDumper> GetListTextDumpers()
        {
            foreach (var list in GetLists())
            foreach (var listDumper in MakeListTextCollectors(list.Key, list.Value))
            {
                yield return listDumper;
            }

            foreach (var list in GetStudioLists())
            foreach (var listDumper in MakeListTextCollectors(list.Key, list.Value, "studio"))
            {
                yield return listDumper;
            }
        }

        protected virtual IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetStudioLists()
        {
            yield return new KeyValuePair<string, AssetDumpColumnInfo>("info", StdStudioAssetCols);
        }

        protected virtual IEnumerable<ITranslationDumper> MakeListTextCollectors(string path,
            AssetDumpColumnInfo assetDumpColumnInfo, string baseDir = "list")
        {
            var rootPath = baseDir.IsNullOrEmpty() ? path : CombinePaths(baseDir, path);

            foreach (var assetBundleName in GetAssetBundleNameListFromPath(rootPath))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    var translations = new OrderedDictionary<string, string>();

                    IDictionary<string, string> AssetDumper()
                    {
                        var done = false;
                        foreach (var tryDump in ListEntryDumpers)
                        {
                            done = tryDump(assetBundleName, assetName, assetDumpColumnInfo, translations);
                            if (done) break;
                        }

                        if (!done) Logger.LogWarning($"Unable to dump '{rootPath}': '{assetBundleName}': {assetName}");

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);

                    if (!assetDumpColumnInfo.CombineWithParentBundle) continue;

                    // treat foo_bar_01 as foo_bar
                    var parentAssetName = assetName;
                    var nameParts = parentAssetName.Split('_');

                    if (nameParts.Length > 1 &&
                        int.TryParse(Path.GetFileNameWithoutExtension(nameParts.Last()), out _))
                    {
                        parentAssetName = string.Join("_", nameParts.Take(nameParts.Length - 1).ToArray()) +
                                          Path.GetExtension(assetName);
                    }

                    var parentFile = BuildAssetFilePath(Path.GetDirectoryName(assetBundleName), parentAssetName);

                    yield return new StringTranslationDumper(parentFile, () => translations);
                }
            }
        }

        protected virtual bool TryDumpExcelData(string assetBundleName, string assetName,
            AssetDumpColumnInfo assetDumpColumnInfo, IDictionary<string, string> translations)
        {
            var excelAsset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, null);
            if (excelAsset is null) return false;

            if (excelAsset.list != null && excelAsset.MaxCell > 0)
            {
                var mappings = new Dictionary<int, int>();
                var skipName = new Dictionary<int, string>();
                assetDumpColumnInfo.NumericMappings.ToList().ForEach(x => mappings[x.Key] = x.Value);

                var headers = ResourceHelper.GetExcelHeaderRow(excelAsset, out var firstRow);
                foreach (var entry in assetDumpColumnInfo.NameMappings)
                {
                    var src = headers.IndexOf(entry.Key);
                    var dest = -1;
                    if (src != -1)
                    {
                        if (!string.IsNullOrEmpty(entry.Value)) dest = headers.IndexOf(entry.Value);

                        mappings[src] = dest;
                        skipName[src] = entry.Key;
                    }
                }

                var itemLookupColumns = new List<int[]>();
                foreach (var entry in assetDumpColumnInfo.ItemLookupColumns)
                {
                    var lookup = ResourceHelper.GetItemLookupColumns(headers, entry);
                    if (lookup.Length > 0) itemLookupColumns.Add(lookup);
                }

                foreach (var mapping in mappings.Where(m => m.Key > -1))
                {
                    for (var i = firstRow; i < excelAsset.list.Count; i++)
                    {
                        var row = excelAsset.GetRow(i);

                        if (row.Count == 0 || row[0] == "no" || row.Count <= mapping.Key) continue;

                        var key = row[mapping.Key];
                        var value = string.Empty;
                        if (skipName.TryGetValue(mapping.Key, out var checkKey) && checkKey == key) continue;

                        if (!ContainsNonAscii(key)) continue;

                        if (mapping.Value > -1 && row.Count > mapping.Value) value = row[mapping.Value];

                        AddLocalizationToResults(translations, key, value);
                    }
                }

                foreach (var mapping in itemLookupColumns)
                {
                    for (var i = 1; i < excelAsset.list.Count; i++)
                    {
                        var row = excelAsset.GetRow(i);
                        var translatedName = ResourceHelper.PerformNameLookup(row, mapping);
                        var key = translatedName.Key;
                        var value = translatedName.Value;
                        AddLocalizationToResults(translations, key, value);
                    }
                }
            }

            return true;
        }

        /*
        protected virtual IEnumerable<KeyValuePair<string, string>> DumpListBytes(byte[] bytes, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            return new Dictionary<string, string>();
        }
        */

        protected virtual IEnumerable<KeyValuePair<string, string>> HandleChaListData(TextAsset asset,
            AssetDumpColumnInfo assetDumpColumnInfo)
        {
            var chaListData = MessagePackSerializer.Deserialize<ChaListData>(asset.bytes);
            foreach (var entry in chaListData.dictList.Values)
            foreach (var mapping in assetDumpColumnInfo.NumericMappings)
            {
                var jpCol = mapping.Key;
                var transCol = mapping.Value;
                if (entry.Count < jpCol) continue;

                var key = entry[jpCol];
                var val = string.Empty;
                if (transCol >= 0 && entry.Count > Math.Max(11, transCol)) val = entry[transCol];

                //Logger.LogWarning($"match: {key}={val}");
                yield return new KeyValuePair<string, string>(key, val);
            }

            foreach (var id in chaListData.dictList.Keys)
            foreach (var mapping in assetDumpColumnInfo.NameMappings)
            {
                var key = chaListData.GetInfo(id, mapping.Key);
                if (!string.IsNullOrEmpty(key))
                {
                    var val = chaListData.GetInfo(id, mapping.Value);
                    yield return new KeyValuePair<string, string>(key,
                        IsValidLocalization(key, val) ? val : string.Empty);
                }
            }
        }

        protected virtual IEnumerable<KeyValuePair<string, string>> DumpListBytes(TextAsset asset,
            AssetDumpColumnInfo assetDumpColumnInfo)
        {
            var scanBytes = asset.bytes.Take(30);
            if (ArrayContains(scanBytes, Encoding.UTF8.GetBytes(ChaListData.ChaListDataMark)))
            {
                foreach (var entry in HandleChaListData(asset, assetDumpColumnInfo))
                {
                    yield return entry;
                }
            }
        }

        protected virtual bool TryDumpTextAsset(string assetBundleName, string assetName,
            AssetDumpColumnInfo assetDumpColumnInfo, IDictionary<string, string> translations)
        {
            var textAsset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, "abdata");

            if (textAsset is null) return false;
            //Logger.LogError($"Trying to dump (text): '{assetBundleName}': {assetName}: {textAsset.name}");

            if (string.IsNullOrEmpty(textAsset.text))
                // may be messagepack in bytes
            {
                foreach (var entry in DumpListBytes(textAsset, assetDumpColumnInfo))
                {
                    var key = entry.Key;
                    var value = entry.Value ?? string.Empty;
                    value = value != "0" ? value : string.Empty;
                    AddLocalizationToResults(translations, key, value);
                }
            }
            else
            {
                foreach (var cols in assetDumpColumnInfo.NumericMappings)
                {
                    var jpCol = cols.Key;
                    var transCol = cols.Value;

                    foreach (var row in TableHelper.SplitTableToRows(textAsset))
                    {
                        var cells = TableHelper.SplitRowToCells(row).ToArray();

                        if (cells.Length <= jpCol) continue;

                        var orig = cells[jpCol];
                        var trans = string.Empty;
                        if (transCol >= 0 && cells.Length > transCol) trans = cells[transCol];

                        AddLocalizationToResults(translations, orig, trans);
                    }
                }
            }

            return true;
        }

        #endregion Lists
    }
}
