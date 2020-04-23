using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    public class KK_AssetDumpHelper : AssetDumpHelper
    {
        public KK_AssetDumpHelper(TextDump plugin) : base(plugin)
        {

            var current = StdTextAssetCols;

            StdStudioAssetCols = new AssetDumpColumnInfo(current.NumericMappings, current.NameMappings, true, current.NameMappings.Select((m) => m.Key));

            AssetDumpGenerators.Add(GetScenarioTextMergers);
            AssetDumpGenerators.Add(GetCommunicatioTextMergers);

        }

        protected IEnumerable<TranslationDumper> GetCommunicatioTextMergers()
        {
            if (!TextDump.IsReadyForFinalDump()) yield break;
            var needle = CombinePaths("", "abdata", "communication", "");
            var paths = TextDump.TranslationsDict.Keys.Where((k) => k.Contains(needle)).ToList();
            paths.Sort();
            paths.Reverse();

            var splitter = new HashSet<char> { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.ToArray();

            var mappings = new Dictionary<string, Dictionary<string, string>>();
            var fileMap = new Dictionary<string, List<string>>();

            foreach (var path in paths)
            {
                var parent = Path.GetFileName(Path.GetDirectoryName(path));
                if (parent is null) continue;
                if (!mappings.TryGetValue(parent, out var perLineMap))
                {
                    mappings[parent] = perLineMap = new Dictionary<string, string>();
                }

                if (!fileMap.TryGetValue(parent, out var perFileList))
                {
                    fileMap[parent] = perFileList = new List<string>();
                }

                perFileList.Add(path);

                foreach (var entry in TextDump.TranslationsDict[path].Where(entry => !entry.Value.IsNullOrEmpty()))
                {
                    AddLocalizationToResults(perLineMap, entry);
                }
            }


            foreach (var perFileMap in fileMap)
            {
                var perLineMap = mappings[perFileMap.Key];
                foreach (var path in perFileMap.Value)
                {
                    var mapPath = path.Substring(TextDump.AssetsRoot.Length).TrimStart('\\', '/');
                    mapPath = CombinePaths(Path.GetDirectoryName(mapPath), Path.GetFileNameWithoutExtension(mapPath));
                    var toUpdate = new HashSet<string>(TextDump.TranslationsDict[path].Where((e) => e.Value.IsNullOrEmpty()).Select((e) => e.Key));
                    if (toUpdate.Count == 0) continue;
                    Dictionary<string, string> Dumper()
                    {
                        return perLineMap.Where((e) => toUpdate.Contains(e.Key))
                            .ToDictionary(entry => entry.Key, entry => entry.Value);
                    }
                    yield return new TranslationDumper(mapPath, Dumper);

                }
            }
        }

        protected IEnumerable<TranslationDumper> GetScenarioTextMergers()
        {
            if (!TextDump.IsReadyForFinalDump()) yield break;
            var needle = CombinePaths("", "abdata", "adv", "scenario", "");
            var paths = TextDump.TranslationsDict.Keys.Where((k) => k.Contains(needle)).ToList();
            paths.Sort();
            paths.Reverse();
            var personalityCheckChars = "01234567890-".ToCharArray();


            var splitter = new HashSet<char> { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.ToArray();

            var mappings = new Dictionary<string, Dictionary<string, string>>();
            var fileMap = new Dictionary<string, List<string>>();
            foreach (var path in paths)
            {
                var parts = path.Split(splitter).ToList();
                var personalityIndex = parts.IndexOf("scenario") + 1;
                if (personalityIndex == 0) continue;
                var personality = parts[personalityIndex];
                var isPersonalityFile = personality.Length > 1 && personality.StartsWith("c") && personalityCheckChars.Contains(personality[1]);
                if (!isPersonalityFile) continue;

                if (!mappings.TryGetValue(personality, out var perLineMap))
                {
                    mappings[personality] = perLineMap = new Dictionary<string, string>();
                }
                if (!fileMap.TryGetValue(personality, out var perFileList))
                {
                    fileMap[personality] = perFileList = new List<string>();
                }

                perFileList.Add(path);

                foreach (var entry in TextDump.TranslationsDict[path].Where(entry => !entry.Value.IsNullOrEmpty()))
                {
                    AddLocalizationToResults(perLineMap, entry);
                }
            }

            foreach (var perFileMap in fileMap)
            {
                var perLineMap = mappings[perFileMap.Key];
                foreach (var path in perFileMap.Value)
                {
                    var mapPath = path.Substring(TextDump.AssetsRoot.Length).TrimStart('\\', '/');
                    mapPath = CombinePaths(Path.GetDirectoryName(mapPath), Path.GetFileNameWithoutExtension(mapPath));
                    var toUpdate = new HashSet<string>(TextDump.TranslationsDict[path].Where((e) => e.Value.IsNullOrEmpty()).Select((e) => e.Key));
                    if (toUpdate.Count == 0) continue;
                    Dictionary<string, string> Dumper()
                    {
                        return perLineMap.Where((e) => toUpdate.Contains(e.Key))
                            .ToDictionary(entry => entry.Key, entry => entry.Value);
                    }
                    yield return new TranslationDumper(mapPath, Dumper);
                }

            }
        }
    }
}
