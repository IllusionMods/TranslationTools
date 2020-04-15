using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using UnityEngine.Assertions;
using System.Text;
using MessagePack;
#if AI
using AIChara;
#endif

namespace IllusionMods
{
    public class AssetDumpHelper : BaseDumpHelper
    {
        protected readonly List<TranslationGenerator> assetDumpGenerators;
        protected List<TryDumpListEntry> ListEntryDumpers { get; }

        public delegate bool TryDumpListEntry(string assetBundleName, string assetName, AssetDumpColumnInfo assetDumpColumnInfo, ref Dictionary<string, string> translations);

        protected readonly AssetDumpColumnInfo stdTextAssetCols;

        public AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            stdTextAssetCols = new AssetDumpColumnInfo(new Dictionary<string, string>
            {
                {"Name", "EN_US" }
            });

            assetDumpGenerators = new List<TranslationGenerator>
            {
                GetCommunicationTextDumpers,
                GetScenarioTextDumpers,
                GetHTextDumpers,
                GetListTextDumpers
            };

            ListEntryDumpers = new List<AssetDumpHelper.TryDumpListEntry>
            {
                TryDumpExcelData,
                TryDumpTextAsset
            };
        }

        protected LocalizationDumpHelper LocalizationDumpHelper => Plugin?.localizationDumpHelper;

        protected virtual string BuildAssetFilePath(string AssetBundleName, string AssetName, string fileName = "translation")
        {
            return CombinePaths(
                Path.GetDirectoryName(AssetBundleName),
                Path.GetFileNameWithoutExtension(AssetBundleName),
                Path.GetFileNameWithoutExtension(AssetName),
                fileName).Replace('/', '\\');
        }

        public virtual IEnumerable<TranslationDumper> GetAssetDumpers()
        {
            Assert.IsNotNull(assetDumpGenerators);
            var dumpers = assetDumpGenerators.ToList();

            while (dumpers.Count > 0)
            {
                var assetDumpGenerator = dumpers.PopFront();
                List<TranslationDumper>entries = new List<TranslationDumper>();
                try
                {
                    foreach (var entry in assetDumpGenerator())
                    {
                        entries.Add(entry);
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError($"Error dumping: {assetDumpGenerator} : {err}");
                }

                foreach (var entry in entries)
                {
                    yield return entry;
                }
            }
        }

#region CommunicationText

        protected virtual TranslationCollector MakeOptionDisplayItemsCollector(string assetBundleName, string assetName)
        {
            Dictionary<string, string> assetDumper()
            {
                var translations = new Dictionary<string, string>();
                var Asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, "abdata");
                if (Asset is null) return translations;

                foreach (var param in Asset.list)
                {
                    string value = string.Empty;
                    if (param.list[0] == "no") continue;
                    for (int i = 1; i < 4; i++)
                    {
                        if (param.list.Count <= i) continue;
                        if (param.list[i].IsNullOrWhiteSpace()) continue;
                        if (param.list[i] == "・・・") continue;

                        try
                        {
                            value = param.list[i + 3];
                        }
                        catch
                        {
                            value = string.Empty;
                        }
                        var key = param.list[i];
                        if (string.IsNullOrEmpty(value))
                        {
                            key = $"//{key}";
                        }
                        ResourceHelper.AddLocalizationToResults(translations, key, value);
                    }
                }
                return translations;
            }
            return assetDumper;
        }

        protected virtual TranslationCollector MakeStandardCommunicationTextCollector(string assetBundleName, string assetName)
        {
            Dictionary<string, string> assetDumper()
            {
                var translations = new Dictionary<string, string>();
                var Asset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, "abdata");
                if (Asset is null) return translations;

                foreach (var param in Asset.list)
                {
                    string value = string.Empty;
                    if (15 <= param.list.Count && !param.list[15].IsNullOrEmpty() && param.list[15] != "テキスト")
                    {
                        try
                        {
                            value = param.list[20];
                        }
                        catch
                        {
                            value = string.Empty;
                        }
                        var key = param.list[15];
                        ResourceHelper.AddLocalizationToResults(translations, key, value);
                    }
                }

                return translations;
            }
            return assetDumper;
        }
        protected virtual IEnumerable<TranslationDumper> GetCommunicationTextDumpers()
        {
            foreach (var AssetBundleName in CommonLib.GetAssetBundleNameListFromPath("communication"))
            {
                if (AssetBundleName.Contains("hit_"))
                    continue;

                foreach (var AssetName in AssetBundleCheck.GetAllAssetName(AssetBundleName))
                {
                    if (AssetName.Contains("speed_"))
                        continue;

                    string filePath = BuildAssetFilePath(AssetBundleName, AssetName);

                    if (AssetName.StartsWith("optiondisplayitems"))
                    {
                        yield return new TranslationDumper(filePath,
                            MakeOptionDisplayItemsCollector(AssetBundleName, AssetName));
                    }
                    else
                    {
                        yield return new TranslationDumper(filePath,
                            MakeStandardCommunicationTextCollector(AssetBundleName, AssetName));
                    }
                }
                if (!string.IsNullOrEmpty(AssetBundleName))
                {
                    yield return new TranslationDumper($"_{nameof(GetCommunicationTextDumpers)}Cleanup", () =>
                    {
                        AssetBundleManager.UnloadAssetBundle(AssetBundleName, false);
                        return new Dictionary<string, string>();
                    });
                }
            }
        }

#endregion CommunicationText

#region ScenarioText

        private string BuildReplacementKey(string assetBundleName, string key)
        {
            return string.Join("|", new string[] { System.IO.Path.GetDirectoryName(assetBundleName), key });
        }
        private void UpdatedReplacementDictionary(string assetBundleName, ref Dictionary<string, KeyValuePair<string, string>> replacementDict, IEnumerable<ADV.ScenarioData.Param> assetParams) =>
            UpdatedReplacementDictionary(assetBundleName, ref replacementDict, assetParams.ToArray());

        private void UpdatedReplacementDictionary(string assetBundleName, ref Dictionary<string, KeyValuePair<string, string>> replacementDict, params ADV.ScenarioData.Param[] assetParams)
        {
            foreach (KeyValuePair<string, KeyValuePair<string, string>> entry in ResourceHelper.BuildReplacements(assetParams))
            {
                replacementDict[BuildReplacementKey(assetBundleName, entry.Key)] = entry.Value;
            }
        }
        private Dictionary<string, KeyValuePair<string, string>> BuildReplacementDictionary(string scenaioRoot)
        {
            Dictionary<string, KeyValuePair<string, string>> result = new Dictionary<string, KeyValuePair<string, string>>();

            List<string> assetBundleNames = CommonLib.GetAssetBundleNameListFromPath(scenaioRoot, true);
            assetBundleNames.Sort();

            foreach (var assetBundleName in assetBundleNames)
            {
                List<string> assetNameList = new List<string>(AssetBundleCheck.GetAllAssetName(assetBundleName));
                assetNameList.Sort();
                foreach (var assetName in assetNameList)
                {
                    var asset = ManualLoadAsset<ADV.ScenarioData>(assetBundleName, assetName, "abdata");
                    if (asset is null) continue;
                    UpdatedReplacementDictionary(assetBundleName, ref result, asset.list);
                }
                //AssetBundleManager.UnloadAssetBundle(assetBundleName, false);
            }
            return result;
        }

        protected virtual IEnumerable<TranslationDumper> GetScenarioTextDumpers()
        {
            HashSet<string> AllJPText = new HashSet<string>();
            foreach (var scenarioRoot in ResourceHelper.GetScenarioDirs())
            {
                var baseChoiceDictionary = BuildReplacementDictionary(scenarioRoot);
                foreach (var assetBundleName in CommonLib.GetAssetBundleNameListFromPath(scenarioRoot, true))
                {
                    foreach (var assetName in AssetBundleCheck.GetAllAssetName(assetBundleName)) //.Where(x => x.StartsWith("personality_voice_"))
                    {
                        var asset = ManualLoadAsset<ADV.ScenarioData>(assetBundleName, assetName, "abdata");
                        if (asset is null || asset.list is null) continue;

                        string FilePath = BuildAssetFilePath(assetBundleName, assetName);
                        var choiceDictionary = new Dictionary<string, KeyValuePair<string, string>>(baseChoiceDictionary);

                        Dictionary<string, string> assetDumper()
                        {
                            var Translations = new Dictionary<string, string>();
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

                                if (param.Command == ADV.Command.Text || param.Command == (ADV.Command)242)
                                {
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
                                            AllJPText.Add(key);
                                            ResourceHelper.AddLocalizationToResults(Translations, key, value);
                                        }
                                    }
                                }
                                else if (param.Command == ADV.Command.Calc)
                                {
                                    //Logger.LogDebug($"DumpScenarioText: {param.Command}: \"{string.Join("\", \"", param.Args)}\"");
                                    if (param.Args.Length >= 3 && ResourceHelper.CalcKeys.Contains(param.Args[0]))
                                    {
                                        var key = ResourceHelper.GetSpecializedKey(param, 2, out string value);
                                        AllJPText.Add(key);
                                        ResourceHelper.AddLocalizationToResults(Translations, key, value);
                                    }
                                }
                                else if (param.Command == ADV.Command.Format)
                                {
                                    //Logger.LogDebug($"DumpScenarioText: {param.Command}: \"{string.Join("\", \"", param.Args)}\"");
                                    if (param.Args.Length >= 2 && ResourceHelper.FormatKeys.Contains(param.Args[0]))
                                    {
                                        var key = param.Args[1];
                                        AllJPText.Add(key);
                                        ResourceHelper.AddLocalizationToResults(Translations, key, string.Empty);

                                        // not sure where localizations are, but they're not in the next arg
                                    }
                                }
                                else if (param.Command == ADV.Command.Choice)
                                {
                                    for (int i = 0; i < param.Args.Length; i++)
                                    {
                                        var key = ResourceHelper.GetSpecializedKey(param, i, out string fallbackValue);
                                        if (!key.IsNullOrEmpty())
                                        {
                                            var value = string.Empty;
                                            if (choiceDictionary.TryGetValue(BuildReplacementKey(assetBundleName, fallbackValue.TrimStart('[').TrimEnd(']')), out KeyValuePair<string, string> entry))
                                            {
                                                key = ResourceHelper.BuildSpecializedKey(param, entry.Key);
                                                value = entry.Value;
                                            }
                                            AllJPText.Add(key);
                                            ResourceHelper.AddLocalizationToResults(Translations, key, value);
                                        }
                                    }
                                }
#if false
                                else if (param.Command == ADV.Command.Switch)
                                {
                                    for (int i = 0; i < param.Args.Length; i++)
                                    {
                                        var key = textResourceHelper.GetSpecializedKey(param, i, out string value);
                                        AllJPText.Add(key);
                                        textResourceHelper.AddLocalizationToResults( Translations, key, value);
                                    }
                                }
#endif
#if false
                                else if (param.Command == ADV.Command.InfoText)
                                {
                                    for (int i = 2; i < param.Args.Length; i += 2)
                                    {
                                        var key = param.args[i];
                                        AllJPText.Add(key);
                                        textResourceHelper.AddLocalizationToResults( Translations, key, string.Empty);
                                    }
                                }
#endif
#if false
                                else if (param.Command == ADV.Command.Jump)
                                {
                                    if (param.Args.Length >= 1 && !AllAscii.IsMatch(param.Args[0]))
                                    {
                                        AllJPText.Add(param.Args[0]);
                                        textResourceHelper.AddLocalizationToResults( Translations, param.Args[0], "Jump");
                                    }
                                }
#endif
                                else
                                {
                                    Logger.LogDebug($"[TextDump] Unsupported command: {param.Command}: {string.Join(",", param.Args.Select((a) => a?.ToString() ?? string.Empty).ToArray())}");
                                }
                            }
                            return Translations;
                        }
                        yield return new TranslationDumper(FilePath, assetDumper);
                    }
                }
            }
        }

#endregion ScenarioText

#region HText

        protected virtual IEnumerable<TranslationDumper> GetHTextDumpers()
        {
            int[] cellsToDump = { 4, 27, 50, 73 };
            foreach (var assetBundleName in CommonLib.GetAssetBundleNameListFromPath("h/list/"))
            {
                foreach (var assetName in AssetBundleCheck.GetAllAssetName(assetBundleName).Where(x => x.StartsWith("personality_voice_")))
                {
                    if (assetName.EndsWith(".txt"))
                    {
                        string FilePath = BuildAssetFilePath(assetBundleName, assetName);

                        Dictionary<string, string> assetDumper()
                        {
                            var Translations = new Dictionary<string, string>();

                            var asset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, "abdata");
                            if (asset is null) return Translations;

                            bool cellHandler(int _, int j, string contents)
                            {
                                if (cellsToDump.Contains(j))
                                {
                                    ResourceHelper.AddLocalizationToResults(Translations, contents, string.Empty);
                                    return true;
                                }
                                return false;
                            }

                            TableHelper.ActOnCells(asset, cellHandler, out TextAssetTableHelper.TableResult _);
                            return Translations;
                        }

                        yield return new TranslationDumper(FilePath, assetDumper);
                    }
                }
            }
        }

#endregion HText

#region Lists

        protected virtual IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            yield return new KeyValuePair<string, AssetDumpColumnInfo>("characustom", stdTextAssetCols);
        }
        protected virtual IEnumerable<TranslationDumper> GetListTextDumpers()
        {
            foreach (var list in GetLists())
            {
                foreach (var listDumper in MakeListTextCollectors(list.Key, list.Value))
                {
                    yield return listDumper;
                }
            }
        }

        protected virtual IEnumerable<TranslationDumper> MakeListTextCollectors(string path, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            var rootPath = $"list/{path}";

            foreach (var assetBundleName in CommonLib.GetAssetBundleNameListFromPath(rootPath, false))
            {
                foreach (var assetName in AssetBundleCheck.GetAllAssetName(assetBundleName))
                {
                    string FilePath = BuildAssetFilePath(assetBundleName, assetName);

                    var translations = new Dictionary<string, string>();
                    Dictionary<string, string> assetDumper()
                    {
                        bool done = false;
                        foreach (var tryDump in ListEntryDumpers)
                        {
                            if (done = tryDump(assetBundleName, assetName, assetDumpColumnInfo, ref translations))
                            {
                                break;
                            }
                        }

                        if (!done)
                        {
                            Logger.LogWarning($"Unable to dump '{rootPath}': '{assetBundleName}': {assetName}");
                        }
                        return translations;
                    }
                    yield return new TranslationDumper(FilePath, assetDumper);

                    if (assetDumpColumnInfo.CombineWithParentBundle)
                    {
                        // treat foo_bar_01 as foo_bar
                        var parentAssetName = assetName;
                        var nameParts = parentAssetName.Split('_');

                        if (nameParts.Length > 1 && int.TryParse(Path.GetFileNameWithoutExtension(nameParts.Last()), out var _))
                        {
                            parentAssetName = string.Join("_", nameParts.Take(nameParts.Length - 1).ToArray()) + Path.GetExtension(assetName);
                        }

                        var parentFile = BuildAssetFilePath(Path.GetDirectoryName(assetBundleName), parentAssetName);

                        yield return new TranslationDumper(parentFile, () => translations);
                    }
                }
            }
        }

        protected virtual bool TryDumpExcelData(string assetBundleName, string assetName, AssetDumpColumnInfo assetDumpColumnInfo, ref Dictionary<string, string> translations)
        {
            ExcelData excelAsset = ManualLoadAsset<ExcelData>(assetBundleName, assetName, null);
            if (excelAsset is null)
            {
                return false;
            }
            if (excelAsset.list != null && excelAsset.MaxCell > 0)
            {
                var headerRow = excelAsset.GetRow(0);
                if (headerRow != null)
                {
                    //Logger.LogError($"Excel Headers?: '{string.Join("', '", headerRow.ToArray())}'");
                }

                //Logger.LogError($"Trying to dump (excel): '{assetBundleName}': {assetName}: {excelAsset.name}");

                Dictionary<int, int> mappings = new Dictionary<int, int>();
                Dictionary<int, string> skipName = new Dictionary<int, string>();
                assetDumpColumnInfo.NumericMappings.ToList().ForEach((x) => mappings[x.Key] = x.Value);

                var headers = excelAsset.GetRow(0);
                foreach (var entry in assetDumpColumnInfo.NameMappings)
                {
                    var src = headers.IndexOf(entry.Key);
                    var dest = -1;
                    if (src != -1)
                    {
                        if (!string.IsNullOrEmpty(entry.Value))
                        {
                            dest = headers.IndexOf(entry.Value);
                        }
                        mappings[src] = dest;
                        skipName[src] = entry.Key;
                    }
                }

                List<int[]> itemLookupColumns = new List<int[]>();
                foreach (var entry in assetDumpColumnInfo.ItemLookupColumns)
                {
                    int[] lookup = this.ResourceHelper.GetItemLookupColumns(headers, entry);
                    if (lookup.Length > 0)
                    {
                        itemLookupColumns.Add(lookup);
                    }
                }
                foreach (var mapping in mappings.Where((m) => m.Key > -1))
                {
                    foreach (var param in excelAsset.list)
                    {
                        var row = param.list;
                        Logger.LogFatal($"item lookup: row='{string.Join("', '", row.ToArray())}'");
                        if (row.Count == 0) continue;
                        if (row[0] == "no") continue;
                        if (row.Count > mapping.Key)
                        {
                            var key = row[mapping.Key];
                            var value = string.Empty;
                            if (skipName.TryGetValue(mapping.Key, out string checkKey) && checkKey == key)
                            {
                                continue;
                            }
                            if (TextResourceHelper.ContainsNonAscii(key))
                            {
                                if (mapping.Value > -1 && row.Count > mapping.Value)
                                {
                                    value = row[mapping.Value];
                                }

                                ResourceHelper.AddLocalizationToResults(translations, key, value);
                            }
                        }
                    }
                }

                foreach (var mapping in itemLookupColumns)
                {
                    foreach (var param in excelAsset.list)
                    {
                        var row = param.list;
                        KeyValuePair<string, string> translatedName = this.ResourceHelper.PerformNameLookup(row, mapping);
                        var key = translatedName.Key;
                        var value = translatedName.Value;
                        ResourceHelper.AddLocalizationToResults(translations, key, value);
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

        private IEnumerable<KeyValuePair<string, string>> HandleChaListData(byte[] bytes, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            ChaListData chaListData = MessagePackSerializer.Deserialize<ChaListData>(bytes);
            foreach (var entry in chaListData.dictList.Values)
            {
                foreach (var mapping in assetDumpColumnInfo.NumericMappings)
                {
                    var jpCol = mapping.Key;
                    var transCol = mapping.Value;
                    if (entry.Count < jpCol)
                    {
                        continue;
                    }
                    var key = entry[jpCol];
                    var val = string.Empty;
                    if (transCol >= 0 && entry.Count > Math.Max(11, transCol))
                    {
                        val = entry[transCol];
                    }
                    ///Logger.LogWarning($"match: {key}={val}");
                    yield return new KeyValuePair<string, string>(key, val);
                }
            }

            foreach (var id in chaListData.dictList.Keys)
            {
                foreach (var mapping in assetDumpColumnInfo.NameMappings)
                {
                    var key = chaListData.GetInfo(id, mapping.Key);
                    if (!string.IsNullOrEmpty(key))
                    {
                        var val = chaListData.GetInfo(id, mapping.Value);
                        yield return new KeyValuePair<string, string>(key, IsValidLocalization(key, val) ? val : string.Empty);
                    }
                }
            }
        }
        protected virtual IEnumerable<KeyValuePair<string, string>> DumpListBytes(byte[] bytes, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            if (TextResourceHelper.ArrayContains<byte>(bytes, Encoding.UTF8.GetBytes(ChaListData.ChaListDataMark)))
            {
                foreach (var entry in HandleChaListData(bytes, assetDumpColumnInfo))
                {
                    yield return entry;
                }
            }
        }

        protected virtual bool TryDumpTextAsset(string assetBundleName, string assetName, AssetDumpColumnInfo assetDumpColumnInfo, ref Dictionary<string, string> translations)
        {
            var textAsset = ManualLoadAsset<TextAsset>(assetBundleName, assetName, "abdata");

            if (textAsset is null)
            {
                return false;
            }
            //Logger.LogError($"Trying to dump (text): '{assetBundleName}': {assetName}: {textAsset.name}");

            if (string.IsNullOrEmpty(textAsset.text))
            {
                // may be messagepack in bytes
                foreach (var entry in DumpListBytes(textAsset.bytes, assetDumpColumnInfo))
                {
                    var key = entry.Key;
                    var value = entry.Value ?? string.Empty;
                    value = value != "0" ? value : string.Empty;
                    ResourceHelper.AddLocalizationToResults(translations, key, value);
                }
            }
            else
            {
                foreach (KeyValuePair<int, int> cols in assetDumpColumnInfo.NumericMappings)
                {
                    var jpCol = cols.Key;
                    var transCol = cols.Value;

                    foreach (var row in TableHelper.SplitTableToRows(textAsset))
                    {
                        var cells = TableHelper.SplitRowToCells(row).ToArray();

                        if (cells.Length <= jpCol)
                        {
                            continue;
                        }
                        string orig = cells[jpCol];
                        string trans = string.Empty;
                        if (transCol >= 0 && cells.Length > transCol)
                        {
                            trans = cells[transCol];
                        }
                        ResourceHelper.AddLocalizationToResults(translations, orig, trans);
                    }
                }
            }
            return true;
        }

#endregion Lists
    }
}
