using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Assertions;
using BepInExLogLevel = BepInEx.Logging.LogLevel;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods
{
    /// <summary>
    ///     Dumps untranslated text to .txt files
    /// </summary>
    [BepInIncompatibility("gravydevsupreme.xunity.autotranslator")]
    [BepInIncompatibility("gravydevsupreme.xunity.resourceredirector")]
    public partial class TextDump
    {
        public delegate void MonoBehaviorScriptFunctionHandler(TextDump sender, EventArgs eventArgs);


        public const string GUID = "com.deathweasel.bepinex.textdump";
        public const string PluginName = "Text Dump";
        public const string Version = "1.2.0";

        private const string FormatStringPlaceholder = "_P_L_A_C_E_H_O_L_D_E_R_";

        private const float NotificationDelay = 10f;

        internal static int DumpLevelMax = 1;
        internal static int DumpLevelReady = 1;
        internal static int DumpLevelCompleted;

        public new static ManualLogSource Logger;

        public static readonly string[] TextAssetLineSplitter = {"\r\n", "\r", "\n"};

        internal static Dictionary<string, Dictionary<string, string>> TranslationsDict =
            new Dictionary<string, Dictionary<string, string>>();

        private static readonly Regex FormatStringRegex = new Regex("\\{[0-9]\\}");

        private static string _dumpRoot;


        internal readonly TextResourceHelper TextResourceHelper;

        private float _nextNotify;
        private bool _writeInProgress;
        internal AssetDumpHelper AssetDumpHelper;
        internal LocalizationDumpHelper LocalizationDumpHelper;
        internal TextAssetTableHelper TextAssetTableHelper;

        protected Func<IEnumerator> CheckReadyToDumpChecker { get; } = null;
        protected Coroutine CheckReadyCoroutine { get; private set; }
        public string CheckReadyNotificationMessage { get; private set; } = string.Empty;


        public static ConfigEntry<bool> Enabled { get; private set; }

        public static string DumpRoot
        {
            get
            {
                while (_dumpRoot is null)
                {
                    _dumpRoot = CombinePaths(Paths.CachePath, $"TextDump-{Path.GetRandomFileName()}");
                    try
                    {
                        Directory.CreateDirectory(_dumpRoot);
                    }
                    catch { }

                    if (!Directory.Exists(_dumpRoot)) _dumpRoot = null;
                }

                return _dumpRoot;
            }
        }

        public static string DumpDestination => CombinePaths(Paths.GameRootPath, "TextDump");
        public static string AssetsRoot => CombinePaths(DumpRoot, "RedirectedResources", "assets", "abdata");
        public static string LocalizationRoot => CombinePaths(DumpRoot, "Text", "Localizations");

        public static ExecutionMode CurrentExecutionMode { get; internal set; } = ExecutionMode.Startup;

        public static bool WriteAfterEachDump { get; internal set; } = false;
        public static bool WriteAfterFinalDump { get; internal set; } = true;

        public static bool ReleaseOnWrite { get; internal set; } = true;
        public bool DumpStarted { get; private set; }
        public bool DumpCompleted { get; private set; }


        public static void LogWithMessage(BepInExLogLevel logLevel, object data)
        {
            Logger?.Log(BepInExLogLevel.Message | logLevel, data);
        }

        internal static bool IsReadyToDump()
        {
            // always ready to try initial dump
            return DumpLevelCompleted == 0 || DumpLevelReady > DumpLevelCompleted;
        }

        internal static bool IsReadyForFinalDump()
        {
            // always ready to try initial dump
            return DumpLevelReady == DumpLevelMax;
        }

        internal static bool AreAllDumpsComplete()
        {
            return DumpLevelCompleted >= DumpLevelMax;
        }

        private void InitPluginSettings()
        {
            Logger = base.Logger;
            Enabled = Enabled ?? Config.Bind("Settings", "Enabled", false, "Whether the plugin is enabled");
        }

        public event MonoBehaviorScriptFunctionHandler TextDumpAwake;
        public event MonoBehaviorScriptFunctionHandler TextDumpMain;
        public event MonoBehaviorScriptFunctionHandler TextDumpUpdate;

        internal void OnTextDumpAwake(EventArgs eventArgs)
        {
            TextDumpAwake?.Invoke(this, eventArgs);
        }

        internal void OnTextDumpMain(EventArgs eventArgs)
        {
            TextDumpMain?.Invoke(this, eventArgs);
        }

        internal void OnTextDumpUpdate(EventArgs eventArgs)
        {
            TextDumpUpdate?.Invoke(this, eventArgs);
        }

        internal void Awake()
        {
            InitPluginSettings();
            if (!Enabled.Value) return;
            if (CurrentExecutionMode == ExecutionMode.BeforeFirstLoad)
            {
                InitialDumpHook.Setup(this);
            }

            OnTextDumpAwake(EventArgs.Empty);
        }

        internal void Main()
        {
            InitPluginSettings();
            if (!Enabled.Value) return;

            if (!DumpStarted && CurrentExecutionMode <= ExecutionMode.Startup)
            {
                DumpText(nameof(Main));
            }

            OnTextDumpMain(EventArgs.Empty);

            if (!AreAllDumpsComplete() && CheckReadyToDumpChecker != null && CheckReadyCoroutine == null)
            {
                CheckReadyCoroutine = StartCoroutine(CheckReadyToDumpChecker());
            }
        }

        internal void Update()
        {
            if (!Enabled.Value || AreAllDumpsComplete()) return;

            OnTextDumpUpdate(EventArgs.Empty);

            if (IsReadyToDump())
            {
                DumpText(nameof(Update));
            }


            if (Time.unscaledTime > _nextNotify && !string.IsNullOrEmpty(CheckReadyNotificationMessage))
            {
                LogWithMessage(BepInExLogLevel.Warning, $"[TextDump] {CheckReadyNotificationMessage}");
                _nextNotify = Time.unscaledTime + NotificationDelay;
            }
        }


        internal void OnDestroy()
        {
            if (CheckReadyCoroutine != null) StopCoroutine(CheckReadyCoroutine);
        }

        private void InitHelpers()
        {
            Assert.IsNotNull(TextResourceHelper, "textResourceHelper not initilized in time");
            TextAssetTableHelper = TextAssetTableHelper ??
                                   new TextAssetTableHelper(new[] {"\r\n", "\r", "\n"}, new[] {"\t"});
            AssetDumpHelper = AssetDumpHelper ?? new AssetDumpHelper(this);
            LocalizationDumpHelper = LocalizationDumpHelper ?? new LocalizationDumpHelper(this);
        }

        private void DumpText(string from)
        {
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(from));
            }

            InitHelpers();
            DumpStarted = true;
            LogWithMessage(BepInExLogLevel.Warning,
                $"[TextDump] Starting dump {DumpLevelCompleted + 1} from {from}. Application may become unresponsive, please wait.");

            if (Directory.Exists(DumpRoot)) Directory.Delete(DumpRoot, true);
            var total = new TranslationCount();

            total += DumpAssets();

            total += DumpLocalizations();

            Logger.LogInfo($"[TextDump] Total lines (translated):{total}");
            DumpCompleted = true;
            DumpLevelCompleted++;
            LogWithMessage(BepInExLogLevel.Info, $"[TextDump] Dump {DumpLevelCompleted} completed.");

            if (WriteAfterEachDump || WriteAfterFinalDump && AreAllDumpsComplete())
            {
                if (_writeInProgress) return;
                StartCoroutine(WriteTranslations());
            }
        }

        private void LogDumpResults(string prefix, string output, TranslationCount before, TranslationCount after)
        {
            var delta = after - before;
            Logger.LogInfo(
                $"[TextDump] lines:{after.Lines,5:D} (new:{delta.Lines,5:D}) translations:{after.TranslatedLines,5:D} (new:{delta.TranslatedLines,5:D}) {prefix} {output}");
        }

        private TranslationCount DumpAssets()
        {
            InitHelpers();
            var folderPath = AssetsRoot;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var total = new TranslationCount();


            foreach (var assetDumper in AssetDumpHelper.GetAssetDumpers())
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
                    Logger.LogError(
                        $"[TextDump] Asset {output}: Error executing {assetDumper.Collector.Method.Name}(): {e.Message}");
                }

                var filePath = Path.Combine(folderPath, $"{output}.txt");

                if (!TranslationsDict.TryGetValue(filePath, out var translations))
                {
                    TranslationsDict[filePath] = translations = new Dictionary<string, string>();
                }

                var beforeCount = new TranslationCount(translations);
                foreach (var localization in results)
                {
                    TextResourceHelper.AddLocalizationToResults(translations, localization);
                }

                var afterCount = new TranslationCount(translations);

                LogDumpResults("Asset", output, beforeCount, afterCount);

                total += afterCount - beforeCount;
            }

            Logger.LogInfo($"[TextDump] Total Asset lines (translated): {total}");
            return total;
        }

        private TranslationCount DumpLocalizations()
        {
            InitHelpers();
            var folderPath = LocalizationRoot;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var total = new TranslationCount();
            foreach (var entry in LocalizationDumpHelper.GetLocalizations())
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
                    Logger.LogError(
                        $"[TextDump] Localization {output}: Error executing {entry.Collector.Method.Name}(): {e.Message}");
                }

                var filePath = Path.Combine(folderPath, $"{output}.txt");

                if (!TranslationsDict.TryGetValue(filePath, out var translations))
                {
                    TranslationsDict[filePath] = translations = new Dictionary<string, string>();
                }

                var beforeCount = new TranslationCount(translations);
                foreach (var localization in results)
                {
                    TextResourceHelper.AddLocalizationToResults(translations, localization);
                }

                var afterCount = new TranslationCount(translations);
                LogDumpResults("Localization", output, beforeCount, afterCount);
                total += afterCount - beforeCount;
            }

            Logger.LogInfo($"[TextDump] Total Localization lines (translated): {total}");
            return total;
        }

        private void RemapTranslations()
        {
            var remappedTranslations = new Dictionary<string, Dictionary<string, string>>();
            foreach (var entry in TranslationsDict)
            {
                var filePath = entry.Key.Replace('/', '\\');
                var mapPath = filePath;
                if (filePath.StartsWith(LocalizationRoot))
                {
                    mapPath = filePath.Substring(LocalizationRoot.Length).TrimStart('\\', '/');

                    mapPath = CombinePaths(LocalizationRoot, LocalizationDumpHelper.LocalizationFileRemap(mapPath));
                }

                if (mapPath != filePath) Logger.LogInfo($"[TextDump] remapping {filePath} => {mapPath}");

                if (!remappedTranslations.TryGetValue(mapPath, out var mappedTranslations))
                {
                    remappedTranslations[mapPath] = mappedTranslations = new Dictionary<string, string>();
                }

                foreach (var translation in entry.Value)
                {
                    TextResourceHelper.AddLocalizationToResults(mappedTranslations, translation);
                }
            }

            TranslationsDict = remappedTranslations;
        }

        private IEnumerator WriteTranslations()
        {
            _writeInProgress = true;
            LogWithMessage(BepInExLogLevel.Warning, $"[TextDump] Writing translation files to {DumpRoot}");
            yield return null;
            RemapTranslations();
            yield return null;
            foreach (var entry in TranslationsDict.ToArray())
            {
                var filePath = entry.Key;
                var translations = entry.Value;
                if (translations.Count <= 0) continue;

                var lines = filePath.StartsWith(AssetsRoot)
                    ? CreateResourceReplacementLines(translations)
                    : CreateLocalizationLines(translations);

                if (ReleaseOnWrite)
                {
                    entry.Value.Clear();
                    TranslationsDict.Remove(entry.Key);
                }

                if (lines.Count > 0) DumpToFile(filePath, lines);
            }

            if (ReleaseOnWrite) TranslationsDict.Clear();
            LogWithMessage(BepInExLogLevel.Info,
                $"[TextDump] Completed writing translation files, moving to {DumpDestination}");

            var retryCount = 0;
            var moveSuccess = false;
            while (retryCount < 10)
            {
                yield return new WaitForSeconds(1);
                retryCount++;
                try
                {
                    MoveDumpToDestination();
                    moveSuccess = true;
                    break;
                }
                catch (Exception err)
                {
                    Logger.LogWarning(
                        $"[TextDump Unable to move {DumpRoot} to {DumpDestination}, will attempt to retry: {err}");
                }
            }

            if (moveSuccess)
            {
                LogWithMessage(BepInExLogLevel.Info,
                    $"[TextDump] Dump can be found in {DumpDestination}");
            }
            else
            {
                LogWithMessage(BepInExLogLevel.Error,
                    $"[TextDump] Unable to write dump to {DumpDestination}, dump can be found in {DumpRoot}");
            }

            _writeInProgress = false;
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            if (sourceDirectory == null) throw new ArgumentNullException(nameof(sourceDirectory));
            if (destinationDirectory == null) throw new ArgumentNullException(nameof(destinationDirectory));

            if (!Directory.Exists(sourceDirectory)) return;

            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            var sourceInfo = new DirectoryInfo(sourceDirectory);

            foreach (var file in sourceInfo.GetFiles())
            {
                file.CopyTo(Path.Combine(destinationDirectory, file.Name), false);
            }

            foreach (var subDir in sourceInfo.GetDirectories())
            {
                CopyDirectory(subDir.FullName, Path.Combine(destinationDirectory, subDir.Name));
            }
        }

        private void MoveDumpToDestination()
        {
            if (Directory.Exists(DumpDestination)) Directory.Delete(DumpDestination, true);
            CopyDirectory(DumpRoot, DumpDestination);
            Directory.Delete(DumpRoot, true);
        }

        private void DumpToFile(string filePath, IEnumerable<string> lines)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            var parent = Path.GetDirectoryName(filePath);
            if (parent != null && !Directory.Exists(parent))
            {
                Directory.CreateDirectory(parent);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllLines(filePath, lines.ToArray());
        }

        private List<string> CreateLocalizationLines(Dictionary<string, string> translations)
        {
            var lines = new List<string>();
            foreach (var localization in translations)
            {
                var key = localization.Key;
                var value = localization.Value;
                value = value.IsNullOrWhiteSpace() ? string.Empty : value;
                if (key.Trim() == value.Trim()) continue;

                if (string.IsNullOrEmpty(value) &&
                    TextResourceHelper.GlobalMappings.TryGetValue(key, out var globalValue))
                {
                    value = globalValue;
                }

                if (string.IsNullOrEmpty(key) || key == value ||
                    string.IsNullOrEmpty(value) && !ContainsNonAscii(key))
                {
                    continue;
                }

                if (!key.StartsWith("r:") && !key.StartsWith("sr:"))
                {
                    key = key.Replace("=", "%3D").Replace("\n", "\\n");
                    value = (value?.Replace("=", "%3D").Replace("\n", "\\n") ?? string.Empty)
                        .TrimEnd(TextResourceHelper.WhitespaceCharacters);

                    var isFormat = FormatStringRegex.IsMatch(key);
                    var regex = isFormat; // || keyHasNewline;

                    if (isFormat)
                    {
                        for (var count = 1; FormatStringRegex.IsMatch(key); count++)
                        {
                            key = FormatStringRegex.Replace(key, FormatStringPlaceholder, 1);
                            value = FormatStringRegex.Replace(value, $"${count}");
                        }
                    }

                    if (regex)
                    {
                        key = Regex.Escape(key);
                        if (isFormat) key = key.Replace(FormatStringPlaceholder, "(.*)");
                    }

                    if (regex) key = (isFormat ? "sr:" : "r:") + "\"^" + key + "$\"";
                }

                if (string.IsNullOrEmpty(value)) key = $"//{key}";
                lines.Add(JoinStrings("=", key, value));
            }

            return lines;
        }

        private static List<string> CreateResourceReplacementLines(Dictionary<string, string> translations)
        {
            var lines = new List<string>();
            foreach (var localization in translations)
            {
                var key = localization.Key.Trim();
                var value = localization.Value.Trim();
                if (key.Contains("\n"))
                {
                    key = $"\"{key.Replace("\n", "\\n").Trim()}\"";
                }

                if (value.Contains("\n"))
                {
                    value = $"\"{value.Replace("\n", "\\n").Trim()}\"";
                }

                value = value.Replace(";", ",");

                if (value.IsNullOrEmpty() && !key.StartsWith("//"))
                {
                    lines.Add($"//{key}=");
                }
                else
                {
                    lines.Add($"{key}={value}");
                }
            }

            return lines;
        }
    }
}
