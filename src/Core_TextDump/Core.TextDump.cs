using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using IllusionMods.Shared;
using UnityEngine;
using UnityEngine.Assertions;
using BepInExLogLevel = BepInEx.Logging.LogLevel;
using static IllusionMods.TextResourceHelper.Helpers;
#if !AI
using Illusion.Extensions;
#endif

namespace IllusionMods
{
    /// <summary>
    ///     Dumps untranslated text to .txt files
    /// </summary>
    [BepInIncompatibility("gravydevsupreme.xunity.autotranslator")]
    [BepInIncompatibility("gravydevsupreme.xunity.resourceredirector")]
    public partial class TextDump
    {
        public delegate void TextDumpEventHandler(TextDump sender, EventArgs eventArgs);

        public delegate bool TranslationPostProcessor(string path, TranslationDictionary translations);


        public const string GUID = "com.deathweasel.bepinex.textdump";
        public const string PluginName = "Text Dump";
        public const string Version = "1.2.3";

        private const string FormatStringPlaceholder = "_P_L_A_C_E_H_O_L_D_E_R_";

        private const float NotificationDelay = 10f;

        internal static int DumpLevelMax = 1;
        internal static int DumpLevelReady = 1;
        internal static int DumpLevelCompleted;

        public new static ManualLogSource Logger;

        public static readonly string[] TextAssetLineSplitter = {"\r\n", "\r", "\n"};

        private static readonly Dictionary<string, TranslationDictionary> _translationsDict =
            new Dictionary<string, TranslationDictionary>();

#if RAW_DUMP_SUPPORT
        internal static Dictionary<string, Func<IEnumerable<byte>>> RawTranslationsDict =
 new Dictionary<string, Func<IEnumerable<byte>>>();
#endif

        internal static List<TranslationPostProcessor> TranslationPostProcessors = new List<TranslationPostProcessor>();

        private static string _dumpRoot;

        private static List<int> AssetDumpLevels { get; } = new List<int>();

        internal readonly TextResourceHelper TextResourceHelper;

        private float _nextNotify;
        private bool _writeInProgress;
        internal AssetDumpHelper AssetDumpHelper;
        internal LocalizationDumpHelper LocalizationDumpHelper;

        private TranslationCount _total = new TranslationCount();
        private TranslationCount _assetTotal = new TranslationCount();
        private TranslationCount _localizationTotal = new TranslationCount();

        protected Func<IEnumerator> CheckReadyToDumpChecker { get; }
        protected Coroutine CheckReadyCoroutine { get; private set; }

        private string _notificationMessage = string.Empty;

        public string NotificationMessage
        {
            get => _notificationMessage;
            private set
            {
                _notificationMessage = value;
                _nextNotify = 0;
            }
        }


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
        public static AssetDumpMode CurrentAssetDumpMode { get; internal set; } = AssetDumpMode.Always;
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

        public event TextDumpEventHandler TextDumpAwake;
        public event TextDumpEventHandler TextDumpMain;
        public event TextDumpEventHandler TextDumpUpdate;
        public event TextDumpEventHandler TextDumpLevelComplete;

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

        internal void OnTextDumpLevelComplete(EventArgs eventArgs)
        {
            TextDumpLevelComplete?.Invoke(this, eventArgs);
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


            HandleNotification();
        }

        private void HandleNotification()
        {
            if (string.IsNullOrEmpty(NotificationMessage) || !(Time.unscaledTime > _nextNotify)) return;
            LogWithMessage(BepInExLogLevel.Warning, $"[TextDump] {NotificationMessage}");
            _nextNotify = Time.unscaledTime + NotificationDelay;
        }


        internal void OnDestroy()
        {
            if (CheckReadyCoroutine != null) StopCoroutine(CheckReadyCoroutine);
        }

        private void InitHelpers()
        {
            Assert.IsNotNull(TextResourceHelper, "textResourceHelper not initilized in time");
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
                $"[TextDump] Starting dump {DumpLevelCompleted + 1}/{DumpLevelMax} from {from}. Application may become unresponsive, please wait.");

            if (Directory.Exists(DumpRoot)) Directory.Delete(DumpRoot, true);

            // if using BeforeFirstLoad it's best not to try doing localizations (if we're dumping more than once).
            var skipLocalizations = CurrentExecutionMode == ExecutionMode.BeforeFirstLoad && DumpLevelCompleted == 0 &&
                                    DumpLevelMax > 1;

            var dumpAssets = CurrentAssetDumpMode == AssetDumpMode.Always ||
                             DumpLevelCompleted < 1 && CurrentAssetDumpMode.HasFlag(AssetDumpMode.FirstOnly) ||
                             DumpLevelReady == DumpLevelMax && CurrentAssetDumpMode.HasFlag(AssetDumpMode.LastOnly) ||
                             CurrentAssetDumpMode == AssetDumpMode.CustomLevels && AssetDumpLevels != null &&
                             AssetDumpLevels.Contains(DumpLevelCompleted + 1);

            if (dumpAssets)
            {
                _total += DumpAssets();
            }

            if (!skipLocalizations)
            {
                _total += DumpLocalizations();
            }

            Logger.LogInfo($"[TextDump] Total lines (translated):{_total}");
            DumpCompleted = true;
            DumpLevelCompleted++;
            LogWithMessage(BepInExLogLevel.Info, $"[TextDump] Dump {DumpLevelCompleted}/{DumpLevelMax} completed.");

            OnTextDumpLevelComplete(EventArgs.Empty);

            if (!WriteAfterEachDump && (!WriteAfterFinalDump || !AreAllDumpsComplete())) return;
            if (_writeInProgress) return;
            StartCoroutine(WriteTranslations());
        }

        private void LogDumpResults(string prefix, string output, TranslationCount before, TranslationCount after)
        {
            var delta = after - before;
            Logger.LogDebug(
                $"[TextDump] {DumpLevelCompleted + 1}/{DumpLevelMax} lines:{after.Lines,4:D} (new:{delta.Lines,4:D}) translations:{after.TranslatedLines,4:D} (new:{delta.TranslatedLines,4:D}) {prefix} {output}");
        }

        private TranslationCount DumpAssets()
        {
            InitHelpers();
            var folderPath = AssetsRoot;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (var assetDumper in AssetDumpHelper.GetAssetDumpers())
            {
                var output = assetDumper.Path;

                switch (assetDumper)
                {
                    case StringTranslationDumper stringDumper:
                    {
                        IDictionary<string, string> results;
                        try
                        {
                            results = stringDumper.Collector();
                        }
                        catch (Exception e)
                        {
                            results = new Dictionary<string, string>();
                            Logger.LogError(
                                $"[TextDump] Asset {output}: Error executing {assetDumper.Collector.Method.Name}(): {e.Message}");
                            Logger.LogDebug($"[TextDump] Asset {output}:\n{e.StackTrace}");
                        }

                        var filePath = Path.Combine(folderPath, $"{output}.txt");

                        var translations = GetTranslationsForPath(filePath);

                        var beforeCount = new TranslationCount(translations);
                        translations.MergeTranslations(results, TextResourceHelper);
                        var afterCount = new TranslationCount(translations);

                        LogDumpResults("Asset", output, beforeCount, afterCount);

                        _assetTotal += afterCount - beforeCount;
                        break;
                    }
#if RAW_DUMP_SUPPORT
                    case RawTranslationDumper rawDumper:
                    {
                        var filePath = Path.Combine(folderPath, $"{output}.bytes");
                        Logger.LogFatal($"RawTranslationsDict[{filePath}] = {rawDumper.Collector}");
                        RawTranslationsDict[filePath] = new Func<IEnumerable<byte>>(rawDumper.Collector);
                        break;
                    }
#endif
                    default:
                        Logger.LogError($"[TextDump] Localization {output}: {assetDumper} is unsupported dumper type");
                        break;
                }
            }

            Logger.LogInfo($"[TextDump] Total Asset lines (translated): {_assetTotal}");

            return _assetTotal;
        }

        private TranslationCount DumpLocalizations()
        {
            InitHelpers();
            var folderPath = LocalizationRoot;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            foreach (var entry in LocalizationDumpHelper.GetLocalizations())
            {
                var output = entry.Path;

                if (!(entry is StringTranslationDumper dumper))
                {
                    Logger.LogError(
                        $"[TextDump] Localization {output}: {entry} is unsupported, must be {nameof(StringTranslationDumper)}");
                    continue;
                }

                IDictionary<string, string> results;
                try
                {
                    results = dumper.Collector();
                }
                catch (Exception e)
                {
                    results = new Dictionary<string, string>();
                    Logger.LogError(
                        $"[TextDump] Localization {output}: Error executing {entry.Collector.Method.Name}(): {e.Message}");
                }

                var filePath = Path.Combine(folderPath, $"{output}.txt");

                var translations = GetTranslationsForPath(filePath);

                var beforeCount = new TranslationCount(translations);
                translations.MergeTranslations(results, TextResourceHelper);
                var afterCount = new TranslationCount(translations);
                LogDumpResults("Localization", output, beforeCount, afterCount);
                _localizationTotal += afterCount - beforeCount;
            }

            Logger.LogInfo($"[TextDump] Total Localization lines (translated): {_localizationTotal}");
            return _localizationTotal;
        }

        public static IEnumerable<string> GetTranslationPaths()
        {
            return _translationsDict.Keys;
        }

        public static TranslationDictionary GetTranslationsForPath(string filePath)
        {
            if (!_translationsDict.TryGetValue(filePath, out var translations))
            {
                _translationsDict[filePath] = translations = new TranslationDictionary();
            }

            Assert.IsTrue(_translationsDict.ContainsKey(filePath));
            return translations;
        }

        private void RemapTranslations()
        {
            var remapNeeded = true;
            while (remapNeeded)
            {
                remapNeeded = false;

                foreach (var entry in _translationsDict.ToList())
                {
                    var filePath = entry.Key.Replace('/', '\\');
                    var mapPath = filePath;
                    if (filePath.StartsWith(LocalizationRoot))
                    {
                        mapPath = filePath.Substring(LocalizationRoot.Length).TrimStart('\\', '/');

                        mapPath = CombinePaths(LocalizationRoot, LocalizationDumpHelper.LocalizationFileRemap(mapPath));
                    }

                    if (mapPath == filePath) continue;

                    Logger.LogInfo($"[TextDump] remapping {filePath} => {mapPath}");

                    var mappedTranslations = GetTranslationsForPath(mapPath);

                    mappedTranslations.MergeTranslations(entry.Value, TextResourceHelper);
                    _translationsDict.Remove(entry.Key);
                    remapNeeded = true;
                }
            }
        }

        private IEnumerator WriteTranslations()
        {
            if (_writeInProgress) yield break;
            NotificationMessage = "Writing translation files, please wait.";
            _writeInProgress = true;
            LogWithMessage(BepInExLogLevel.Warning, $"[TextDump] Writing translation files to {DumpRoot}");
            yield return null;
            PostProcessTranslations();

            yield return null;
            var count = 0;
            foreach (var entry in _translationsDict.ToArray())
            {
                count++;
                if (count % 100 == 0) yield return null;
                var filePath = entry.Key;
                var translations = entry.Value;

                var lines = filePath.StartsWith(AssetsRoot)
                    ? CreateResourceReplacementLines(translations)
                    : CreateLocalizationLines(translations);

                if (ReleaseOnWrite && AreAllDumpsComplete())
                {
                    entry.Value.Clear();
                    _translationsDict.Remove(entry.Key);
                }

                if (lines.Count > 0) DumpToFile(filePath, lines);
            }

#if RAW_DUMP_SUPPORT
            foreach (var entry in RawTranslationsDict.ToArray())
            {
                count++;
                if ((count % 100) == 0) yield return null;
                var filePath = entry.Key;
                var collector = entry.Value;
                byte[] bytes = null;
                try
                {
                    bytes = collector()?.ToArray();
                }
                catch (Exception e)
                {
                    Logger.LogMessage($"[TextDump] Unable to dump Raw Asset {filePath}: {collector}: {e}");
                }

                if (ReleaseOnWrite)
                {
                    RawTranslationsDict.Remove(entry.Key);
                }

                if (bytes == null || bytes.Length == 0) continue;
                DumpToFile(filePath, bytes);
            }
#endif

            NotificationMessage = $"Moving translation files to {DumpDestination}, please wait.";
            if (ReleaseOnWrite) _translationsDict.Clear();
            LogWithMessage(BepInExLogLevel.Info,
                $"[TextDump] Completed writing translation files, moving to {DumpDestination}");

            var moveSuccess = false;
            var retryCount = 0;
            while (retryCount < 10)
            {
                yield return new WaitForSeconds(retryCount * 1.5f);
                retryCount++;
                try
                {
                    if (Directory.Exists(DumpDestination)) Directory.Delete(DumpDestination, true);
                    break;
                }
                catch (Exception err)
                {
                    Logger.LogWarning(
                        $"[TextDump] Unable to remove existing {DumpDestination}, will attempt to retry: {err}");
                }
            }

            // try using move, then try with copy
            foreach (var useCopy in new[] {false, true})
            {
                if (moveSuccess) break;
                if (!useCopy && Directory.Exists(DumpDestination)) continue;
                retryCount = 0;
                while (retryCount < 10)
                {
                    yield return new WaitForSeconds(retryCount * 1.5f);
                    retryCount++;
                    try
                    {
                        MoveDumpToDestination(useCopy);
                        moveSuccess = true;
                        break;
                    }
                    catch (Exception err)
                    {
                        Logger.LogWarning(
                            $"[TextDump] Unable to move {DumpRoot} to {DumpDestination}, will attempt to retry: {err}");
                    }
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
            NotificationMessage = string.Empty;
        }

        private void PostProcessTranslations()
        {
            var postProcessors = TranslationPostProcessors.ToList();

            var tryCount = 0;
            var retryPostProcessors = new List<TranslationPostProcessor>();
            while (postProcessors.Count > 0)
            {
                tryCount++;
                if (tryCount > 3) break;
                while (postProcessors.Count > 0)
                {
                    var postProcessor = postProcessors.PopFront();
                    var retry = false;
                    foreach (var _ in _translationsDict.Where(e => !postProcessor(e.Key, e.Value)))
                    {
                        retry = true;
                    }

                    if (retry)
                    {
                        retryPostProcessors.Add(postProcessor);
                    }
                }

                postProcessors.AddRange(retryPostProcessors);
            }

            RemapTranslations();
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

        private void MoveDumpToDestination(bool useCopy = false)
        {
            if (useCopy)
            {
                if (Directory.Exists(DumpDestination)) Directory.Delete(DumpDestination, true);

                CopyDirectory(DumpRoot, DumpDestination);
                Directory.Delete(DumpRoot, true);
            }
            else
            {
                Directory.Move(DumpRoot, DumpDestination);
            }
        }

        private void DumpToFile<T>(string filePath, IEnumerable<T> value, Action<string, T[]> writeAction)
        {
            Logger.LogDebug($"[TextDump] writing {filePath}");
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

            writeAction(filePath, value.ToArray());
        }

#if RAW_DUMP_SUPPORT
        private void DumpToFile(string filePath, IEnumerable<byte> bytes)
        {
            DumpToFile(filePath, bytes, File.WriteAllBytes);
        }
#endif
        private void DumpToFile(string filePath, IEnumerable<string> lines)
        {
            DumpToFile(filePath, lines, File.WriteAllLines);
        }

        private List<string> CreateLocalizationLines(TranslationDictionary translations)
        {
            var formatStringRegex = LocalizationDumpHelper.FormatStringRegex;
            var lines = new List<string>();
            var scopeLines = new List<string>();

            foreach (var scope in translations.Scopes)
            {
                scopeLines.Clear();
                foreach (var localization in translations.GetScope(scope))
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

                    LocalizationDumpHelper.PrepareLineForDump(ref key, ref value);


                    if (!key.StartsWith("r:") && !key.StartsWith("sr:"))
                    {
                        key = key.Replace("=", "%3D");
                        value = (value?.Replace("=", "%3D") ?? string.Empty)
                            .TrimEnd(TextResourceHelper.WhitespaceCharacters);

                        var isFormat = formatStringRegex.IsMatch(key);
                        var regex = isFormat;
                        if (isFormat)
                        {
                            for (var count = 1; formatStringRegex.IsMatch(key); count++)
                            {
                                var match = formatStringRegex.Match(key);
                                key = formatStringRegex.Replace(key, FormatStringPlaceholder, 1);
                                try
                                {
                                    value = new Regex(Regex.Escape(match.Value)).Replace(value, $"${count}", 1);
                                }
                                catch (ArgumentException err)
                                {
                                    Logger.LogWarning(
                                        $"Unable to correctly create replacements for {match.Value} in {value}, check your output files: {err}");
                                }
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
                    scopeLines.Add(JoinStrings("=", key, value));
                }

                if (scopeLines.Count <= 0) continue;
                if (lines.Count > 0) lines.Add("");
                if (scope != -1) lines.Add($"#set level {scope}");
                lines.AddRange(scopeLines);
                if (scope != -1) lines.Add($"#unset level {scope}");
            }

            return lines;
        }

        private List<string> CreateResourceReplacementLines(IDictionary<string, string> translations)
        {
            var lines = new List<string>();
            foreach (var localization in translations)
            {
                var key = localization.Key.Trim();
                var value = localization.Value.Trim();

                AssetDumpHelper.PrepareLineForDump(ref key, ref value);

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
