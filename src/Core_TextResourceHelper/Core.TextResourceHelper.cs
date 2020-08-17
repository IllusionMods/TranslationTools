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
        protected static readonly string ChoiceDelimiter = ",";
        protected static readonly string SpecializedKeyDelimiter = ":";
        public readonly char[] WhitespaceCharacters = {' ', '\t'};

        public const char OptionSafeComma = '\u201a';

        private TextAssetTableHelper _tableHelper;

        protected ManualLogSource Logger
        {
            get
            {
                if (_logger != null) return _logger;
                return (_logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name));
            }
        }

        public TextAssetTableHelper TableHelper => _tableHelper ?? (_tableHelper = GetTableHelper());

        public virtual int XUnityLanguageToGameLanguage(string xUnityLanguage)
        {
            return -1;
        }

        protected TextResourceHelper() { }

        public virtual bool IsValidLocalization(string original, string localization)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            return !string.IsNullOrEmpty(localization) && localization != "0" && localization != original;
        }

        public virtual IEnumerable<int> GetSupportedExcelColumns(string calculatedModificationPath, ExcelData asset)
        {
            return new int[0];
        }

        public virtual List<string> GetExcelHeaderRow(ExcelData asset, out int firstRow)
        {
            firstRow = 0;
            var headerRow = asset.GetRow(firstRow++);
            var numEmpty = headerRow.Count(h => h.IsNullOrWhiteSpace());
            if (headerRow.Count == numEmpty ||
                headerRow.Count > 1 && (
                    headerRow[1].IsNullOrWhiteSpace() && (
                        headerRow[0].StartsWith("Ｈ") ||
                        headerRow[0] == "主人公")))

            {
                headerRow = asset.GetRow(firstRow++);
            }
            else if (numEmpty > 3 || (numEmpty/(headerRow.Count * 1.0)) > 0.49)
            {
                var testRow = asset.GetRow(firstRow);
                var testData = asset.GetRow(firstRow + 1);
                if (testRow.Count > 1)
                {
                    if ((testRow[0] == "表示順番" || testRow[0] == "管理番号") ||
                        (testData.Count > 1 && int.TryParse(testData[0], out _) && !int.TryParse(testRow[0], out _)))
                    {
                        headerRow = testRow;
                        firstRow++;
                    }
                }

            }

            return headerRow;
        }

        public List<string> GetExcelHeaderRow(ExcelData asset)
        {
            return GetExcelHeaderRow(asset, out _);
        }

        protected virtual TextAssetTableHelper GetTableHelper()
        {
            return new TextAssetTableHelper(new[] {"\r\n", "\r", "\n"}, new[] {"\t"});
        }

        protected virtual bool IsValidExcelRowTranslationKey(string key)
        {
            return key != "0";
        }
        public virtual IEnumerable<string> GetExcelRowTranslationKeys(string assetName, List<string> row, int i)
        {
            if (row == null || row.Count <= i) yield break;
            var key = row[i];
            if (IsValidExcelRowTranslationKey(key)) yield return key;
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

        public bool IsSupportedCommand(Command command)
        {
            return SupportedCommands.Contains(command);
        }


        public virtual string BuildSpecializedKey(ScenarioData.Param param, string toTranslate)
        {
            return Helpers.JoinStrings(SpecializedKeyDelimiter, param.Command.ToString().ToUpperInvariant(),
                toTranslate);
        }

        // Certain commands encode multiple pieces of data into their strings
        // we only want to expose the part that should be translated
        // use prefixing to signal to resource replacement when this is needed
        public virtual string GetSpecializedKey(ScenarioData.Param param, int i, out string toTranslate)
        {
            var key = toTranslate = param.Args[i];
            if (key.IsNullOrEmpty() || param.Command == Command.Text)
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
            else
            {
                // does not used specialized key
                return key;
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

        // For commands that encode multiple pieces of data into their strings
        // keep all the extra data from the asset file and only replace the
        // displayed section (otherwise just returns the passed translation)
        public virtual string GetSpecializedTranslation(ScenarioData.Param param, int i, string translation)
        {
            if (param.Command == Command.Choice)
            {
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

        public virtual IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            return new Dictionary<string, AssetDumpColumnInfo>();
        }

        public virtual void AddLocalizationToResults(IDictionary<string, string> results, string origTxt,
            string transTxt)
        {
            if (origTxt.IsNullOrWhiteSpace()) return;
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

#endif
        public string PrepareTranslationForReplacement(ExcelData asset, string translated)
        {
            return IsOptionDisplayItemAsset(asset.name) ? translated.Replace(',', OptionSafeComma) : translated;
        }

        public virtual void InitializeHelper() { }


        public virtual bool IsValidStringArrayParamAssetTranslation(string orig, string translated)
        {
            return false;
        }
    }
}
