using System;
using BepInEx.Logging;
#if !HS
using ADV;
using System.Collections.Generic;
using System.Linq;

#endif

namespace IllusionMods
{
    public partial class TextResourceHelper
    {
        private static ManualLogSource _logger;
        protected static readonly string ChoiceDelimiter = ",";
        protected static readonly string SpecializedKeyDelimiter = ":";
        public readonly char[] WhitespaceCharacters = {' ', '\t'};

        protected static ManualLogSource Logger =>
            _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(nameof(TextResourceHelper));


        public virtual bool IsValidLocalization(string original, string localization)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            return !string.IsNullOrEmpty(localization) && localization != "0" && localization != original;
        }

#if !HS

        public Dictionary<string, string> GlobalMappings { get; } = new Dictionary<string, string>();

        // Contains strings that should not be replaced in `Command.Text` based resources
        public HashSet<string> TextKeysBlacklist { get; protected set; }

        // Only dump `Command.Calc` strings if `params.Args[0]` listed here
        public HashSet<string> CalcKeys { get; protected set; }

        // Only dump `Command.Format` strings if `params.Args[0]` listed here
        public HashSet<string> FormatKeys { get; protected set; }

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
                    return Helpers.JoinStrings(ChoiceDelimiter, translation,
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
                if (IsReplacement(param) && param.Args.Length > 2 && param.Args[0].StartsWith("sel"))
                {
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

        public virtual void AddLocalizationToResults(Dictionary<string, string> results, string origTxt,
            string transTxt)
        {
            if (origTxt.IsNullOrWhiteSpace()) return;
            var localization = CleanLocalization(origTxt, transTxt);
            if (!results.ContainsKey(origTxt) || !string.IsNullOrEmpty(localization))
            {
                results[origTxt] = localization;
            }
        }

        public virtual void AddLocalizationToResults(Dictionary<string, string> results,
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
    }
}
