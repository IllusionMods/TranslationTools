using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Illusion.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Assertions;

namespace IllusionMods
{
    public partial class TextDump
    {
        public const string GUID = "com.deathweasel.bepinex.textdump";
        public const string PluginName = "Text Dump";
        public const string Version = "1.2.0";

        internal readonly TextResourceHelper textResourceHelper = null;
        internal TextAssetTableHelper textAssetTableHelper = null;
        internal AssetDumpHelper assetDumpHelper = null;
        internal LocalizationDumpHelper localizationDumpHelper = null;

        public static ConfigEntry<bool> Enabled { get; private set; }

        public static string DumpRoot => CombinePaths(Paths.GameRootPath, "TextDump");
        public static string AssetsRoot => CombinePaths(DumpRoot, "RedirectedResources", "assets", "abdata");
        public static string LocalizationRoot => CombinePaths(DumpRoot, "Text", "Localizations");

        public static ExecutionMode CurrentExecutionMode { get; internal set; } = ExecutionMode.Startup;

        public static bool WriteOnDump { get; internal set; } = true;

        public static bool ReleaseOnWrite { get; internal set; } = true;

        internal static bool readyToDump = true;

        new public static ManualLogSource Logger;

        public static readonly string[] textAssetLineSplitter = new[] { "\r\n", "\r", "\n" };

        public static void LogWithMessage(BepInEx.Logging.LogLevel logLevel, object data) => Logger?.Log(BepInEx.Logging.LogLevel.Message | logLevel, data);
        public bool DumpStarted { get; private set; } = false;
        public bool DumpCompleted { get; private set; } = false;

        internal static bool IsReadyToDump()
        {
            return readyToDump;
        }

        internal void Main()
        {
            Logger = base.Logger;
            Enabled = Config.Bind("Settings", "Enabled", false, "Whether the plugin is enabled");

            if (Enabled.Value && !DumpStarted && CurrentExecutionMode == TextDump.ExecutionMode.Startup)
            {
                Logger.LogInfo("[TextDump] Starting dump from Main");
                DumpText();
            }
        }

        public static string CombinePaths(params string[] parts)
        {
            return TextResourceHelper.CombinePaths(parts);
        }

        private void InitHelpers()
        {
            Assert.IsNotNull(textResourceHelper, "textResourceHelper not initilized in time");
            textAssetTableHelper = textAssetTableHelper ?? new TextAssetTableHelper(new string[] { "\r\n", "\r", "\n" }, new string[] { "\t" });
            assetDumpHelper = assetDumpHelper ?? new AssetDumpHelper(this);
            localizationDumpHelper = localizationDumpHelper ?? new LocalizationDumpHelper(this);
        }
        private void DumpText()
        {
            InitHelpers();
            DumpStarted = true;

            if (Directory.Exists(DumpRoot))
            {
                Directory.Delete(DumpRoot, true);
            }
            var total = 0;

            total += DumpAssets();

            total += DumpLocalizations();

            Logger.LogInfo($"[TextDump] Total lines:{total}");
            DumpCompleted = true;

            if (WriteOnDump)
            {
                WriteTranslations();
            }
        }

        private int DumpAssets()
        {
            InitHelpers();
            string folderPath = AssetsRoot;
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            int total = 0;

            foreach (var assetDumper in assetDumpHelper.GetAssetDumpers())
            {
                var output = assetDumper.Path;
                Dictionary<string, string> results;
                try
                {
                    results = assetDumper.Collector();
                }
                catch (Exception e)
                {
                    results = new Dictionary<string, string>();
                    Logger.LogError($"[TextDump] Asset {output}: Error executing {assetDumper.Collector.Method.Name}(): {e.Message}");
                }
                string filePath = Path.Combine(folderPath, $"{output}.txt");

                if (!TranslationsDict.TryGetValue(filePath, out Dictionary<string, string> translations))
                {
                    TranslationsDict[filePath] = translations = new Dictionary<string, string>();
                }
                var origSize = translations.Count;
                foreach (var localization in results)
                {
                    textResourceHelper.AddLocalizationToResults(translations, localization);
                }
                var added = translations.Count - origSize;
                Logger.LogInfo($"[TextDump] Localization {output} lines:{added}");
                total += added;
            }

            /*
            total += DumpCommunicationText();
            total += DumpScenarioText();
            total += DumpHText();
            total += DumpLists();
            */
            Logger.LogInfo($"[TextDump] Total Asset lines: {total}");
            return total;
        }

        private int DumpLocalizations()
        {
            InitHelpers();
            HashSet<string> AllJPText = new HashSet<string>();
            string FolderPath = LocalizationRoot;
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);

            foreach (var entry in localizationDumpHelper.GetLocalizations())
            {
                var output = entry.Path;
                Dictionary<string, string> results;
                try
                {
                    results = entry.Collector();
                }
                catch (Exception e)
                {
                    results = new Dictionary<string, string>();
                    Logger.LogError($"[TextDump] Localization {output}: Error executing {entry.Collector.Method.Name}(): {e.Message}");
                }
                string filePath = Path.Combine(FolderPath, $"{output}.txt");

                if (!TranslationsDict.TryGetValue(filePath, out Dictionary<string, string> translations))
                {
                    TranslationsDict[filePath] = translations = new Dictionary<string, string>();
                }
                foreach (var localization in results)
                {
                    textResourceHelper.AddLocalizationToResults(translations, localization);
                    AllJPText.Add(localization.Key);
                }
                Logger.LogInfo($"[TextDump] Localization {output} lines: {translations.Count}");
            }
            Logger.LogInfo($"[TextDump] Total Localization lines:{AllJPText.Count}");
            return AllJPText.Count;
        }

        internal static Dictionary<string, Dictionary<string, string>> TranslationsDict = new Dictionary<string, Dictionary<string, string>>();

        private void RemapTranslations()
        {
            var remappedTranslations = new Dictionary<string, Dictionary<string, string>>();
            foreach (var entry in TranslationsDict)
            {
                var filePath = entry.Key.Replace('/', '\\');
                var mapPath = filePath;
                var translations = entry.Value;
                if (filePath.StartsWith(LocalizationRoot))
                {
                    mapPath = filePath.Substring(LocalizationRoot.Length).TrimStart('\\', '/');

                    mapPath = CombinePaths(LocalizationRoot, localizationDumpHelper.LocalizationFileRemap(mapPath));
                }

                if (mapPath != filePath)
                {
                    Logger.LogInfo($"[TextDump] remapping {filePath} => {mapPath}");
                }

                if (!remappedTranslations.TryGetValue(mapPath, out var mappedTranslations))
                {
                    remappedTranslations[mapPath] = mappedTranslations = new Dictionary<string, string>();
                }
                foreach (var translation in entry.Value)
                {
                    textResourceHelper.AddLocalizationToResults(mappedTranslations, translation);
                }
            }
            TranslationsDict = remappedTranslations;
        }
        private void WriteTranslations()
        {
            if (Directory.Exists(DumpRoot))
            {
                Directory.Delete(DumpRoot, true);
            }
            RemapTranslations();

            foreach (var entry in TranslationsDict.ToArray())
            {
                var filePath = entry.Key;
                var translations = entry.Value;
                if (translations.Count > 0)
                {
                    List<string> lines;
                    if (filePath.StartsWith(AssetsRoot))
                    {
                        lines = CreateResourceReplacementLines(translations);
                    }
                    else
                    {
                        lines = CreateLocalizationLines(translations);
                    }
                    if (ReleaseOnWrite)
                    {
                        entry.Value.Clear();
                        TranslationsDict.Remove(entry.Key);
                    }
                    if (lines.Count > 0)
                    {
                        DumpToFile(filePath, lines);
                    }
                }
            }
            if (ReleaseOnWrite)
            {
                TranslationsDict.Clear();
            }
        }
        private void DumpToFile(string filePath, IEnumerable<string> lines)
        {
            var parent = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(parent))
                Directory.CreateDirectory(parent);
            if (File.Exists(filePath))
                File.Delete(filePath);
            File.WriteAllLines(filePath, lines.ToArray());
        }
        private List<string> CreateLocalizationLines(Dictionary<string, string> translations)
        {
            List<string> lines = new List<string>();
            foreach (var localization in translations)
            {
                var key = localization.Key;
                var value = localization.Value;
                value = value.IsNullOrWhiteSpace() ? string.Empty : value;
                if (key.Trim() == value.Trim()) continue;

                if (string.IsNullOrEmpty(value) && textResourceHelper.GlobalMappings.TryGetValue(key, out var globalValue))
                {
                    value = globalValue;
                }
                if (string.IsNullOrEmpty(key) || key == value || (string.IsNullOrEmpty(value) && !TextResourceHelper.ContainsNonAscii(key)))
                {
                    continue;
                }
                if (!key.StartsWith("r:") && !key.StartsWith("sr:"))
                {
                    key = key.Replace("=", "%3D").Replace("\n", "\\n");
                    value = value?.Replace("=", "%3D")?.Replace("\n", "\\n").TrimEnd(textResourceHelper.WhitespaceCharacters);

                    bool isFormat = FormatStringRegex.IsMatch(key);
                    bool regex = isFormat;// || keyHasNewline;

                    if (isFormat)
                    {
                        for (int count = 1; FormatStringRegex.IsMatch(key); count++)
                        {
                            key = FormatStringRegex.Replace(key, FormatStringPlaceholder, 1);
                            value = FormatStringRegex.Replace(value, $"${count}");
                        }
                    }

                    if (regex)
                    {
                        key = Regex.Escape(key);
                        if (isFormat)
                        {
                            key = key.Replace(FormatStringPlaceholder, "(.*)");
                        }
                    }
                    if (regex)
                    {
                        key = (isFormat ? "sr:" : "r:") + "\"^" + key + "$\"";
                    }
                }

                if (string.IsNullOrEmpty(value))
                {
                    key = $"//{key}";
                }
                lines.Add(string.Join("=", new[] { key, value }));
            }
            return lines;
        }

        private static List<string> CreateResourceReplacementLines(Dictionary<string, string> translations)
        {
            List<string> lines = new List<string>();
            foreach (var tl in translations)
            {
                string JP = tl.Key.Trim();
                string ENG = tl.Value.Trim();
                if (JP.Contains("\n"))
                    JP = $"\"{JP.Replace("\n", "\\n").Trim()}\"";
                if (ENG.Contains("\n"))
                    ENG = $"\"{ENG.Replace("\n", "\\n").Trim()}\"";
                ENG = ENG.Replace(";", ",");

                if (ENG.IsNullOrEmpty() && !JP.StartsWith("//"))
                    lines.Add($"//{JP}=");
                else
                    lines.Add($"{JP}={ENG}");
            }
            return lines;
        }

        private static readonly Regex FormatStringRegex = new Regex($"\\{{[0-9]\\}}");

        private const string FormatStringPlaceholder = "_P_L_A_C_E_H_O_L_D_E_R_";

        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : UnityEngine.Object
        {
            return TextResourceHelper.ManualLoadAsset<T>(bundle, asset, manifest);
        }

        public static void UnloadBundles() => TextResourceHelper.UnloadBundles();

        public static List<string> GetAllAssetBundles() => CommonLib.GetAssetBundleNameListFromPath(".", true);
    }
}
