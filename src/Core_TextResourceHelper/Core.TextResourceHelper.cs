using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
#if !HS
using ADV;
#endif

namespace IllusionMods
{
    public partial class TextResourceHelper : BaseHelperFactory<TextResourceHelper>, IHelper
    {
        private static ManualLogSource _logger;

        public const char OptionSafeComma = '\u201a';
        protected static readonly string ChoiceDelimiter = ",";
        protected static readonly string SpecializedKeyDelimiter = ":";
        public readonly char[] WhitespaceCharacters = {' ', '\t'};

        private TextAssetTableHelper _tableHelper;
        private ResourceMappingHelper _resourceMappingHelper;

        internal new static ManualLogSource Logger => _logger ?? GetLogger<TextResourceHelper>();

        private static readonly IEnumerable<int> EmptyIndexes = new int[0];

        protected TextResourceHelper()
        {
            _logger = base.Logger;
        }

        public TextAssetTableHelper TableHelper => _tableHelper ?? (_tableHelper = GetTableHelper());

        public ResourceMappingHelper ResourceMappingHelper =>
            _resourceMappingHelper ?? (_resourceMappingHelper = GetResourceMappingHelper());
        
        public virtual void InitializeHelper() { }

        public virtual int XUnityLanguageToGameLanguage(string xUnityLanguage)
        {
            return -1;
        }

        public virtual bool IsValidLocalization(string original, string localization)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var trimmedOrig = original.Trim();
            var trimmedLocal = localization?.Trim();
            return !int.TryParse(trimmedOrig, out _) &&
                   !string.IsNullOrEmpty(trimmedLocal) &&
                   !string.Equals("0", trimmedLocal) &&
                   !string.Equals(trimmedOrig, trimmedLocal);
        }

        public virtual IEnumerable<int> GetSupportedExcelColumns(string calculatedModificationPath, ExcelData asset, out int firstRow)
        {
            try
            {
                GetExcelHeaderRows(asset, out firstRow);
            }
            catch
            {
                firstRow = 0;
            }
            return new int[0];
        }

        public virtual List<List<string>> GetExcelHeaderRows(ExcelData asset, out int firstRow)
        {
            var headerRows = new List<List<string>>();
            firstRow = 0;
            var headerRow = asset.GetRow(firstRow++);
            headerRows.Add(headerRow);

            var numEmpty = headerRow.Count(h => h.IsNullOrWhiteSpace());
            var hiddenIndex = headerRow.IndexOf("非表示オブジェクト");
            if (headerRow.Count == numEmpty ||
                headerRow.Count > 1 && headerRow[1].IsNullOrWhiteSpace() && (
                    headerRow[0].StartsWith("Ｈ") ||
                    headerRow[0] == "主人公"))

            {
                headerRows.Add(asset.GetRow(firstRow++));
            }
            else if (hiddenIndex == -1 && numEmpty > 3 || numEmpty / (headerRow.Count * 1.0) > 0.49)
            {
                var testRow = asset.GetRow(firstRow);
                var testData = asset.GetRow(firstRow + 1);
                if (testRow.Count > 1)
                {
                    if (testRow[0] == "表示順番" || testRow[0] == "管理番号" ||
                        testData.Count > 1 && int.TryParse(testData[0], out _) && !int.TryParse(testRow[0], out _))
                    {
                        headerRows.Add(testRow);
                        firstRow++;
                    }
                }
            }
            else if (hiddenIndex > 0)
            {
                var testRow = asset.GetRow(firstRow);
                if (testRow.Count > hiddenIndex && testRow[0].IsNullOrEmpty() && !testRow[hiddenIndex].IsNullOrEmpty())
                {
                    var dataRow = asset.GetRow(firstRow + 1);
                    if (dataRow.Count > 0 && !dataRow[0].IsNullOrEmpty())
                    {
                        firstRow++;
                        headerRows.Add(testRow);
                    }
                }
            }


            var rowNum = -1;
            foreach (var row in headerRows)
            {
                rowNum++;
                Logger.DebugLogDebug(
                    $"{nameof(GetExcelHeaderRows)}: {asset.name}: firstRow={firstRow}, headerRows[{rowNum}]='{string.Join("', '", row.ToArray())}'");
            }

            return headerRows;
        }

        public List<List<string>> GetExcelHeaderRows(ExcelData asset)
        {
            return GetExcelHeaderRows(asset, out _);
        }

        public virtual IEnumerable<string> GetExcelRowTranslationKeys(string assetName, List<string> row, int i)
        {
            if (row == null || row.Count <= i) yield break;
            var key = row[i];
            if (!IsValidExcelRowTranslationKey(key)) yield break;

            // specialized match much come first
            if (IsRandomNameListAsset(assetName)) yield return $"NAME[{i}]:{key}";

            yield return key;
        }

        public virtual bool IsOptionDisplayItemAsset(string assetName)
        {
            return assetName.StartsWith("optiondisplayitems", StringComparison.OrdinalIgnoreCase);
        }

        public virtual bool IsOptionDisplayItemPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var parts = Helpers.SplitPath(Path.GetDirectoryName(path));
            return parts?.LastOrDefault()?.StartsWith("optiondisplayitems", StringComparison.OrdinalIgnoreCase) ??
                   false;
        }

        public virtual bool IsRandomNameListAsset(string assetName)
        {
            return false;
        }

        public string PrepareTranslationForReplacement(ExcelData asset, string translated)
        {
            return IsOptionDisplayItemAsset(asset.name) ? translated.Replace(',', OptionSafeComma) : translated;
        }


        public virtual bool IsValidStringArrayParamAssetTranslation(string orig, string translated)
        {
            return false;
        }

        protected virtual TextAssetTableHelper GetTableHelper()
        {
            return new TextAssetTableHelper(new[] {"\r\n", "\r", "\n"}, new[] {"\t"});
        }

        protected virtual ResourceMappingHelper GetResourceMappingHelper()
        {
            return new ResourceMappingHelper();
        }

        protected virtual bool IsValidExcelRowTranslationKey(string key)
        {
            return key != "0";
        }

#if !HS

        public Dictionary<string, string> GlobalMappings { get; } = new Dictionary<string, string>();

        // Contains strings that should not be replaced in `Command.Text` based resources
        public HashSet<string> TextKeysBlacklist { get; protected set; } = new HashSet<string>();

        // Only dump `Command.Calc` strings if `params.Args[0]` listed here
        public HashSet<string> CalcKeys { get; protected set; } = new HashSet<string>();

        // Only dump `Command.Format` strings if `params.Args[0]` listed here
        public HashSet<string> FormatKeys { get; protected set; } = new HashSet<string>();

        protected HashSet<Command> SupportedCommands { get; } = new HashSet<Command>
        {
            Command.Text
        };

        // do NOT put Text in here
        public HashSet<Command> SpecializedKeyCommands { get; } = new HashSet<Command>();

        public HashSet<Command> RawKeyDisabledCommands { get; } = new HashSet<Command>();

        internal virtual Dictionary<string, IEnumerable<string>> GetSettingsStrings()
        {
            return new Dictionary<string, IEnumerable<string>>
            {
                {nameof(TextKeysBlacklist), TextKeysBlacklist},
                {nameof(CalcKeys), CalcKeys},
                {nameof(FormatKeys), FormatKeys},
                {nameof(SupportedCommands), SupportedCommands.Select(c => c.ToString())},
                {nameof(SpecializedKeyCommands), SpecializedKeyCommands.Select(c=>c.ToString())},
                {nameof(GetRandomNameDirs), GetRandomNameDirs().ToList()},
                {nameof(GetScenarioDirs), GetScenarioDirs().ToList()}
            };
        }

        public bool IsSupportedCommand(Command command)
        {
            return SupportedCommands.Contains(command);
        }

        protected virtual string GetSpecializedKeyPrefix(ScenarioData.Param param)
        {
            return param.Command.ToString().ToUpperInvariant();
        }

        public virtual string BuildSpecializedKey(ScenarioData.Param param, string toTranslate)
        {
            return Helpers.JoinStrings(SpecializedKeyDelimiter, GetSpecializedKeyPrefix(param), toTranslate);
        }

        // Certain commands encode multiple pieces of data into their strings
        // we only want to expose the part that should be translated
        // use prefixing to signal to resource replacement when this is needed
        public virtual string GetSpecializedKey(ScenarioData.Param param, int i, out string toTranslate)
        {

            var key = toTranslate = param.Args[i];
            if (key.IsNullOrEmpty() || !SpecializedKeyCommands.Contains(param.Command))
            {
                return key;
            }

            if (param.Command == Command.Choice)
            {
                if (!key.Contains(ChoiceDelimiter))
                {
                    // only choices that contain delimiters need translation
                    return string.Empty;
                }

                toTranslate = key.Split(ChoiceDelimiter.ToCharArray())[0];
            }
            return BuildSpecializedKey(param, toTranslate);
        }

        public virtual string GetSpecializedKey(object obj, string defaultValue)
        {
            return defaultValue;
        }

        public string GetSpecializedKey(ScenarioData.Param param, int i)
        {
            return GetSpecializedKey(param, i, out _);
        }

        public virtual IEnumerable<string> GetTranslationKeys(object obj, string defaultValue)
        {
            var key = GetSpecializedKey(obj, defaultValue);
            yield return key;
            if (IsRawKeyDisabled(obj) || key == defaultValue) yield break;
            yield return defaultValue;
        }

        protected virtual bool IsRawKeyDisabled(object obj)
        {
            return false;
        }

        public IEnumerable<string> GetTranslationKeys(ScenarioData.Param param, int i)
        {
            var key = GetSpecializedKey(param, i, out var rawKey);
            yield return key;
            if (RawKeyDisabledCommands.Contains(param.Command) || key == rawKey) yield break;
            yield return rawKey;
        }

        // For commands that encode multiple pieces of data into their strings
        // keep all the extra data from the asset file and only replace the
        // displayed section (otherwise just returns the passed translation)
        public virtual string GetSpecializedTranslation(ScenarioData.Param param, int i, string translation)
        {
            if (param.Command != Command.Choice) return translation;
            try
            {
                return Helpers.JoinStrings(ChoiceDelimiter,
                    translation.Replace(ChoiceDelimiter[0], OptionSafeComma),
                    param.Args[i].Split(ChoiceDelimiter.ToCharArray(), 2)[1]);
            }
            catch
            {
                // something went wrong, return original below
            }

            return translation;
        }

        public virtual bool IsReplacement(ScenarioData.Param param)
        {
            return false;
        }

        public Dictionary<string, KeyValuePair<string, string>> BuildReplacements(
            IEnumerable<ScenarioData.Param> assetList)
        {
            return BuildReplacements(assetList.ToArray());
        }

        public virtual Dictionary<string, KeyValuePair<string, string>> BuildReplacements(
            params ScenarioData.Param[] assetList)
        {
            var result = new Dictionary<string, KeyValuePair<string, string>>();

            foreach (var param in assetList)
            {
                if (!IsReplacement(param) || param.Args.Length <= 2 || !param.Args[0].StartsWith("sel")) continue;
                var key = param.Args[0];
                var entry = new KeyValuePair<string, string>(param.Args[1], param.Args[2]);
                if (result.TryGetValue(key, out var existing))
                {
                    if (existing.Key != entry.Key || existing.Value != entry.Value)
                    {
                        Logger.LogWarning(
                            $"Duplicate replacement key: {key} in (replacing {result[key]} with {entry}'");
                    }
                }

                result[key] = entry;
            }

            return result;
        }

        public virtual IEnumerable<string> GetScenarioDirs()
        {
            yield return "adv/scenario";
        }

        public virtual IEnumerable<string> GetRandomNameDirs()
        {
            yield break;
        }

        public virtual IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            return new Dictionary<string, AssetDumpColumnInfo>();
        }

        public virtual void AddLocalizationToResults(IDictionary<string, string> results, string origTxt,
            string transTxt)
        {
            if (origTxt.IsNullOrWhiteSpace() || double.TryParse(origTxt.Trim(), out _)) return;
            var localization = CleanLocalization(origTxt, transTxt);
            if (!results.ContainsKey(origTxt) || !string.IsNullOrEmpty(localization))
            {
                results[origTxt] = localization;
            }
        }

        public virtual void AddLocalizationToResults(IDictionary<string, string> results,
            KeyValuePair<string, string> mapping)
        {
            AddLocalizationToResults(results, mapping.Key, mapping.Value);
        }

        public virtual string CleanLocalization(string origTxt, string localization)
        {
            return IsValidLocalization(origTxt, localization)
                ? localization.TrimEnd(WhitespaceCharacters)
                : string.Empty;
        }

        public virtual IEnumerable<KeyValuePair<string, string>> DumpListBytes(byte[] bytes,
            AssetDumpColumnInfo assetDumpColumnInfo)
        {
            return new Dictionary<string, string>();
        }

        public virtual int[] GetItemLookupColumns(List<string> headers, string nameLookupColumnName)
        {
            var srcCol = headers.IndexOf(nameLookupColumnName);
            return srcCol == -1 ? new int[0] : new[] {srcCol, -1};
        }

        public virtual KeyValuePair<string, string> PerformNameLookup(List<string> row, int[] lookup)
        {
            var key = string.Empty;
            var val = string.Empty;
            if (lookup[0] > -1 && row.Count > lookup[0]) key = row[lookup[0]];
            if (lookup[1] > -1 && row.Count > lookup[1]) val = row[lookup[1]];

            if (val.IsNullOrEmpty())
            {
                GlobalMappings.TryGetValue(key, out val);
            }

            return new KeyValuePair<string, string>(key, val);
        }

        public virtual IEnumerable<int> GetScenarioCommandTranslationIndexes(Command command)
        {
            switch (command)
            {
                case Command.Text:
                {
                    yield return 0;
                    yield return 1;
                    break;
                }

                case Command.Calc:
                {
                    yield return 2;
                    break;
                }

                case Command.Format:
                {
                    yield return 1;
                    break;
                }

            }
            yield break;
        }

#else // HS
        internal virtual Dictionary<string, IEnumerable<string>> GetSettingsStrings() =>
            new Dictionary<string, IEnumerable<string>>();

        public virtual string GetSpecializedKey(object obj, string defaultValue) => defaultValue;
#endif
    }
}
