using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Assertions;
using BepInExLogLevel = BepInEx.Logging.LogLevel;
using static IllusionMods.TextResourceHelper.Helpers;

namespace IllusionMods.Shared.TextDumpBase
{
    public abstract class BaseTextDumpPlugin : BaseUnityPlugin
    {
        public delegate void TextDumpEventHandler(BaseTextDumpPlugin sender, EventArgs eventArgs);

        public delegate bool TranslationPostProcessor(string path, TranslationDictionary translations);

        private const float NotificationDelay = 12f;

        protected static readonly Dictionary<string, TranslationDictionary> TranslationsDict =
            new Dictionary<string, TranslationDictionary>();

        public new static ManualLogSource Logger;

        internal static readonly List<TranslationPostProcessor> TranslationPostProcessors =
            new List<TranslationPostProcessor>();

        private string _pluginName;
        private string _pluginVersion;
        private string _pluginNameInternal;

        private static string _dumpRoot;

        private static string _dumpDestination;

        protected readonly IEnumerator CheckReadyToDumpDelay = new WaitForSecondsRealtime(1f);
        private float _nextNotify;

        internal static readonly bool IsStudio = Application.productName == Constants.StudioProcessName;

        private string _notificationMessage = string.Empty;
        protected bool DumpCompleted;

        protected bool DumpStarted;

        public static ConfigEntry<bool> Enabled { get; private set; }

        protected bool WriteInProgress { get; private set; }

        public static bool ReleaseOnWrite { get; internal set; } = true;

        public string NotificationMessage
        {
            get => _notificationMessage;
            protected set
            {
                _notificationMessage = value;
                _nextNotify = 0;
            }
        }

        internal TextResourceHelper TextResourceHelper { get; private set; }

        protected void SetTextResourceHelper(TextResourceHelper helper) => TextResourceHelper = helper;

        protected static T CreateHelper<T>() where T : IHelper
        {
            return BaseHelperFactory<T>.Create<T>();
        }


        protected virtual string DumpRoot
        {
            get
            {
                while (_dumpRoot is null)
                {
                    _dumpRoot = CombinePaths(Paths.CachePath, $"{_pluginName.Replace(" ", "-")}-{Path.GetRandomFileName()}");
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

        protected virtual string DumpDestination => _dumpDestination ??
                                            (_dumpDestination = CombinePaths(Paths.GameRootPath,
                                                _pluginName.Replace(" ", "")));

        protected void InitPluginSettings(string pluginName, string pluginVersion, string pluginNameInternal)
        {
            _pluginName = pluginName;
            _pluginVersion = pluginVersion;
            _pluginNameInternal = pluginNameInternal;
            Logger = Logger ?? base.Logger;
            Enabled = Enabled ?? Config.Bind("Settings", "Enabled", false, "Whether the plugin is enabled");
        }

        protected abstract void InitPluginSettings();

        protected void HandleNotification()
        {
            if (string.IsNullOrEmpty(NotificationMessage) || Time.unscaledTime < _nextNotify) return;
            LogWithMessage(BepInExLogLevel.Warning, NotificationMessage);
            StartCoroutine(UpdateNotificationTime());

        }

        private IEnumerator UpdateNotificationTime()
        {
            _nextNotify = float.MaxValue;
            // don't reset time until next frame
            yield return null;
            _nextNotify = Time.unscaledTime + NotificationDelay;
        }

        protected abstract List<string> CreateLines(string filePath, TranslationDictionary translations);

        protected virtual bool IsSafeToRelease()
        {
            return true;
        }

        protected IEnumerator WriteTranslations()
        {
            if (WriteInProgress) yield break;
            NotificationMessage = "Writing translation files, please wait.";
            WriteInProgress = true;
            LogWithMessage(BepInExLogLevel.Warning, $"Writing translation files to {DumpRoot}");
            yield return null;
            PostProcessTranslations();

            yield return null;
            var count = 0;
            foreach (var entry in TranslationsDict.ToArray())
            {
                count++;
                if (count % 100 == 0) yield return null;
                var filePath = entry.Key;
                var translations = entry.Value;

                var lines = CreateLines(filePath, translations);

                if (ReleaseOnWrite && IsSafeToRelease())
                {
                    entry.Value.Clear();
                    TranslationsDict.Remove(entry.Key);
                }

                if (lines.Count <= 0)
                {
                    Logger.LogDebug($"No lines to dump for {filePath}, skipping.");
                    continue;
                }

                DumpToFile(filePath, lines);
            }

            yield return WriteAdditionalTranslations(count);


            NotificationMessage = $"Moving translation files to {DumpDestination}, please wait.";
            if (ReleaseOnWrite) TranslationsDict.Clear();
            LogWithMessage(BepInExLogLevel.Info,
                $"Completed writing translation files, moving to {DumpDestination}");

            var moveSuccess = false;
            var retryCount = 0;
            var retryDelay = 0f;
            while (retryCount < 10)
            {
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
                retryDelay = retryCount * 1.5f;

                try
                {
                    if (Directory.Exists(DumpDestination)) Directory.Delete(DumpDestination, true);
                    break;
                }
                catch (Exception err)
                {
                    Logger.LogWarning(
                        $"Unable to remove existing {DumpDestination}, will attempt to retry in {retryDelay}s: {err.Message}");
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
                    yield return new WaitForSeconds(retryDelay);
                    retryCount++;
                    retryDelay = retryCount * 1.5f;
                    try
                    {
                        MoveDumpToDestination(useCopy);
                        moveSuccess = true;
                        break;
                    }
                    catch (Exception err)
                    {
                        Logger.LogWarning(
                            $"Unable to move {DumpRoot} to {DumpDestination}, will attempt to retry in {retryDelay}s: {err}");
                    }
                }
            }

            if (moveSuccess)
            {
                LogWithMessage(BepInExLogLevel.Info,
                    $"Dump can be found in {DumpDestination}");
            }
            else
            {
                LogWithMessage(BepInExLogLevel.Error,
                    $"Unable to write dump to {DumpDestination}, dump can be found in {DumpRoot}");
            }

            WriteInProgress = false;
            NotificationMessage = string.Empty;
        }

        protected virtual IEnumerator WriteAdditionalTranslations(int initialCount)
        {
            yield break;
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
                    foreach (var _ in TranslationsDict.Where(e => !postProcessor(e.Key, e.Value)))
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

        protected virtual void RemapTranslations() { }

        protected abstract Version GetGameVersion();

        protected virtual IEnumerable<string> GetHeaderLines(params string[] extra)
        {
            yield return "//";
            yield return
                $"// Dumped for {Constants.GameName} v{GetGameVersion()} ({Utilities.GetCurrentExecutableName()}) by {_pluginNameInternal} v{_pluginVersion}";
            foreach (var line in extra)
            {
                yield return $"// {line}";
            }

            yield return "//";
        }

        protected virtual void DumpToFile(string filePath, IEnumerable<string> lines)
        {
            var allLines = new[] {GetHeaderLines().AsEnumerable(), lines}.SelectMany(s => s);
            DumpToFile(filePath, allLines, File.WriteAllLines);
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

        protected void DumpToFile<T>(string filePath, IEnumerable<T> value, Action<string, T[]> writeAction)
        {
            Logger.LogDebug($"{nameof(DumpToFile)}: writing {filePath}");
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

        protected static void LogWithMessage(BepInExLogLevel logLevel, object data)
        {
            Logger?.Log(BepInExLogLevel.Message | logLevel, data);
        }

        public static IEnumerable<string> GetTranslationPaths()
        {
            return TranslationsDict.Keys;
        }

        public static TranslationDictionary GetTranslationsForPath(string filePath)
        {
            var translations = TranslationsDict.GetOrInit(filePath);

            Assert.IsTrue(TranslationsDict.ContainsKey(filePath));
            return translations;
        }

    }
}
