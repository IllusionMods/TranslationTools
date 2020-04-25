using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class KK_AssetDumpHelper : AssetDumpHelper
    {
        private static readonly char[] PathSplitter =
            new HashSet<char> {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}.ToArray();
        public KK_AssetDumpHelper(TextDump plugin) : base(plugin)
        {

            var current = StdTextAssetCols;

            StdStudioAssetCols = new AssetDumpColumnInfo(current.NumericMappings, current.NameMappings, true, current.NameMappings.Select((m) => m.Key));

            AssetDumpGenerators.Add(GetScenarioTextMergers);
            AssetDumpGenerators.Add(GetCommunicationTextMergers);

        }

#if false
        // TODO: finish this and see if it works without breaking things
        protected virtual IEnumerable<TranslationDumper> GetClubInfoDumpers()
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

        protected IEnumerable<TranslationDumper> GetCommunicationTextMergers()
        {
            if (!TextDump.IsReadyForFinalDump()) yield break;
            var needle = CombinePaths("", "abdata", "communication", "");
            var paths = TextDump.TranslationsDict.Keys.Where((k) => k.Contains(needle)).ToList();
            paths.Sort();
            paths.Reverse();

            var splitter = new HashSet<char> { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.ToArray();

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

                foreach (var entry in TextDump.TranslationsDict[path].Where(entry => !entry.Value.IsNullOrEmpty()))
                {
                    AddLocalizationToResults(personalityMap, entry);
                }
            }

            foreach (var translationDumper in BuildTranslationMergers(fileMaps, mappings)) yield return translationDumper;

        }

        protected IEnumerable<TranslationDumper> GetScenarioTextMergers()
        {
            if (!TextDump.IsReadyForFinalDump()) yield break;
            var needle = CombinePaths("", "abdata", "adv", "scenario", "");
            var paths = TextDump.TranslationsDict.Keys.Where((k) => k.Contains(needle)).ToList();
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
                var isPersonalityFile = personality.Length > 1 && personality.StartsWith("c") && personalityCheckChars.Contains(personality[1]);
                if (!isPersonalityFile) continue;

                if (!mappings.TryGetValue(personality, out var personalityMap))
                {
                    mappings[personality] = personalityMap = new Dictionary<string, string>(new TrimmedStringComparer());
                }
                if (!fileMaps.TryGetValue(personality, out var personalityFiles))
                {
                    fileMaps[personality] = personalityFiles = new List<string>();
                }
                personalityFiles.Add(path);

                foreach (var entry in TextDump.TranslationsDict[path].Where(entry => !entry.Value.IsNullOrEmpty()))
                {
                    AddLocalizationToResults(personalityMap, entry);
                }
            }

            foreach (var translationDumper in BuildTranslationMergers(fileMaps, mappings)) yield return translationDumper;
        }

        protected IEnumerable<TranslationDumper> BuildTranslationMergers(Dictionary<string, List<string>> fileMaps, Dictionary<string, Dictionary<string, string>> mappings)
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

                    var toUpdate = new HashSet<string>(TextDump.TranslationsDict[path]
                        .Where((e) => e.Value.IsNullOrWhiteSpace()).Select((e) => e.Key));

                    if (toUpdate.Count == 0) continue;

                    Dictionary<string, string> Dumper()
                    {
                        var result = new Dictionary<string, string>();

                        foreach (var key in toUpdate)
                        {
                            if (personalityMap.TryGetValue(key, out var match))
                            {
                                AddLocalizationToResults(result, key, match);
                            }
                        }

                        return result;
                    }

                    yield return new TranslationDumper(mapPath, Dumper);
                }
            }
        }
    }
}
