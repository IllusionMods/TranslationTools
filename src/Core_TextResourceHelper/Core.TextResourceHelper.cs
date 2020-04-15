using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
#if !HS
using ADV;
#endif

namespace IllusionMods
{
    public partial class TextResourceHelper
    {
        private static ManualLogSource _logger = null;
        private static readonly HashSet<string> loadedBundles = new HashSet<string>();
        protected static readonly string ChoiceDelimiter = ",";
        protected static readonly string SpecializedKeyDelimiter = ":";
        public readonly char[] WhitespaceCharacters = new[] { ' ', '\t' };

        protected static ManualLogSource Logger => _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(typeof(TextResourceHelper).Name);
        public static string CombinePaths(params string[] parts)
        {
            string result = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                if (i == 0)
                {
                    result = parts[i];
                }
                else
                {
                    result = System.IO.Path.Combine(result, parts[i]);
                }
            }
            return result.Replace('/', '\\');
        }

        public virtual bool IsValidLocalization(string original, string localization)
        {
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

        protected Dictionary<Command, bool> SupportedCommands = new Dictionary<Command, bool>() {
            { ADV.Command.Text, true }
        };
        public static bool ContainsNonAscii(string input) => input.ToCharArray().Any((c) => c > sbyte.MaxValue);

        public bool IsSupportedCommand(Command command)
        {
            if (SupportedCommands.TryGetValue(command, out bool result))
            {
                return result;
            }
            // default to false
            return false;
        }

        public virtual string BuildSpecializedKey(ScenarioData.Param param, string toTranslate)
        {
            return string.Join(SpecializedKeyDelimiter, new string[] { param.Command.ToString().ToUpperInvariant(), toTranslate });
        }
        // Certain commands encode multiple pieces of data into their strings
        // we only want to expose the part that should be translated
        // use prefixing to signal to resource replacement when this is needed
        public virtual string GetSpecializedKey(ScenarioData.Param param, int i, out string toTranslate)
        {
            string key = toTranslate = param.Args[i];
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

        public string GetSpecializedKey(ScenarioData.Param param, int i) => GetSpecializedKey(param, i, out string _);

        // For commands that encode multiple pieces of data into their strings
        // keep all the extra data from the asset file and only replace the
        // displayed section (otherwise just returns the passed translation)
        public string GetSpecializedTranslation(ScenarioData.Param param, int i, string translation)
        {
            if (param.Command == Command.Choice)
            {
                try
                {
                    return string.Join(ChoiceDelimiter,
                                new string[] { translation, param.Args[i].Split(ChoiceDelimiter.ToCharArray(), 2)[1] });
                }
                catch
                {
                    // something went wrong, return original below
                }
            }

            return translation;
        }

        virtual public bool IsReplacement(ScenarioData.Param param) => false;

        public Dictionary<string, KeyValuePair<string, string>> BuildReplacements(IEnumerable<ScenarioData.Param> assetList) => BuildReplacements(assetList.ToArray());

        virtual public Dictionary<string, KeyValuePair<string, string>> BuildReplacements(params ScenarioData.Param[] assetList)
        {
            Dictionary<string, KeyValuePair<string, string>> result = new Dictionary<string, KeyValuePair<string, string>>();

            foreach (ScenarioData.Param param in assetList)
            {
                if (IsReplacement(param) && param.Args.Length > 2 && param.Args[0].StartsWith("sel"))
                {
                    var key = param.Args[0];
                    KeyValuePair<string, string> entry = new KeyValuePair<string, string>(param.Args[1], param.Args[2]);
                    if (result.TryGetValue(key, out KeyValuePair<string, string> existing))
                    {
                        if (existing.Key != entry.Key || existing.Value != entry.Value)
                        {
                            Logger.LogWarning($"Duplicate replacement key: {key} in (replacing {result[key]} with {entry}'");
                        }
                    }
                    result[key] = entry;
                }
            }
            return result;
        }

        virtual public IEnumerable<string> GetScenarioDirs()
        {
            yield return "adv/scenario";
        }

        virtual public IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            return new Dictionary<string, AssetDumpColumnInfo>();
        }

        /*
        public virtual IEnumerable<KeyValuePair<string, TranslationCollector>> GetLocalizations()
        {
            foreach (var entry in AutoLocalizers)
            {
                yield return new TranslationDumper(
                    CombinePaths("AutoLocalizers", entry.Key),
                    () => entry.Value);
            }
        }
        */

        public virtual void AddLocalizationToResults(Dictionary<string, string> results, string origTxt, string transTxt)
        {
            if (!origTxt.IsNullOrWhiteSpace())
            {
                var localization = CleanLocalization(origTxt, transTxt);
                if (!string.IsNullOrEmpty(localization) || !results.ContainsKey(origTxt))
                {
                    results[origTxt] = localization;
                }
            }
        }

        public virtual void AddLocalizationToResults(Dictionary<string, string> results, KeyValuePair<string, string> mapping)
        {
            AddLocalizationToResults(results, mapping.Key, mapping.Value);
        }

        public virtual string CleanLocalization(string origTxt, string localization)
        {
            if (IsValidLocalization(origTxt, localization))
            {
                return localization.TrimEnd(WhitespaceCharacters);
            }
            return string.Empty;
        }

        public virtual IEnumerable<KeyValuePair<string, string>> DumpListBytes(byte[] bytes, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            return new Dictionary<string, string>();
        }

        public virtual int[] GetItemLookupColumns(List<string> headers, string nameLookupColumnName)
        {
            return new int[0];
        }

        public virtual KeyValuePair<string, string> PerformNameLookup(List<string> row, int[] lookup)
        {
            return new KeyValuePair<string, string>(string.Empty, string.Empty);
        }

        //public virtual string LocalizationFileRemap(string outputFile) => outputFile;

#endif
        public static bool ArrayContains<T>(IEnumerable<T> haystack, IEnumerable<T> needle) where T : IComparable
        {
            var haystackList = haystack.ToList();
            var haystackLength = haystackList.Count;
            var needleList = needle.ToList();
            var needleLength = needleList.Count;

            int start = 0;
            // while first character exists in remaining haystack
            while ((start = haystackList.IndexOf(needleList[0], start)) != -1)
            {
                if ((start + needleLength) > haystackLength)
                {
                    // can't fit in remaining bytes
                    break;
                }
                bool found = true;
                for (int i = 1; i < needleLength; i++)
                {
                    if (needleList[i].CompareTo(haystackList[start + i]) != 0)
                    {
                        // mismatch
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    // loop completed without mismatch
                    return true;
                }
            }
            return false;
        }

        public static void UnloadBundles()
        {
            var bundles = loadedBundles.ToArray();
            loadedBundles.Clear();
            foreach (var assetBundle in bundles)
            {
                AssetBundleManager.UnloadAssetBundle(assetBundle, false);
            }
        }
        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : UnityEngine.Object
        {
            loadedBundles.Add(bundle);
#if AI && LOCALIZE
            return AIProject.AssetUtility.LoadAsset<T>(bundle, asset, manifest);
#else
#if HS
            var _ = asset;
            _ = manifest;
            return null;
#else
            AssetBundleManager.LoadAssetBundleInternal(bundle, false, manifest);
            var assetBundle = AssetBundleManager.GetLoadedAssetBundle(bundle, out string error, manifest);
            if (!string.IsNullOrEmpty(error))
            {
                Logger?.LogError($"ManualLoadAsset: {error}");
            }
            var result = assetBundle.m_AssetBundle.LoadAsset<T>(asset);
            return result;
#endif
#endif
        }
    }
}
