using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using HarmonyLib;
using IllusionMods.Shared;
using IllusionMods.Shared.TextDumpBase;
using KKAPI;
using KKAPI.Studio;
using Manager;
using Sideloader.AutoResolver;
using Studio;
using UnityEngine;
using UnityEngine.Assertions;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Constants;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using static IllusionMods.TextResourceHelper.Helpers;

#if AI||HS2
using AIChara;

#endif

namespace IllusionMods
{
    [BepInDependency(PluginData.Identifier, PluginData.Version)]
    [BepInDependency(Sideloader.Sideloader.GUID, Sideloader.Sideloader.Version)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public partial class ModTextDump : BaseTextDumpPlugin
    {
        public const string GUID = "com.illusionmods.translationtools.mod_text_dump";
        public const string PluginName = "Mod Text Dump";
        public const string Version = "0.5.0";

        private const string FilePattern = "_-_-_-_-_-_";

        private static readonly ChaListDefine.KeyType[] MakerGroupingKeys =
        {
            ChaListDefine.KeyType.MainManifest, ChaListDefine.KeyType.MainAB, ChaListDefine.KeyType.MainTexAB,
            ChaListDefine.KeyType.ThumbAB
        };

        private static readonly List<int> TranslationScopes = new List<int>();

        private static readonly HashSet<int> HandledScopes = new HashSet<int>();

        private static readonly string[] StudioDumpPrefix =
            {string.Empty, $"#set exe {Constants.StudioProcessName}", string.Empty};

        private readonly Regex _whitespaceRemover = new Regex(@"\s+", Constants.DefaultRegexOptions);

        private Coroutine _checkReadyCoroutine;

        private SimpleTextTranslationCache _currentTranslationCache;

        private bool _readyToDump;

        public static string StudioRoot { get; private set; }

        public static string MakerRoot { get; private set; }

        protected override string DumpDestination =>
            string.Concat(base.DumpDestination, IsStudio ? "-Studio" : "-MainGame");

        public void Awake()
        {
            InitPluginSettings();
            //if (!IsStudio) MakerAPI.MakerFinishedLoading += MakerAPI_MakerFinishedLoading;
        }

        internal void Update()
        {
            if (DumpCompleted || !Enabled.Value) return;
            if (DumpStarted)
            {
                if (WriteInProgress) HandleNotification();
                return;
            }

            if (_checkReadyCoroutine == null)
            {
                _checkReadyCoroutine = StartCoroutine(CheckReadyToDump());
            }

            if (_readyToDump)
            {
                DumpText();
            }


            HandleNotification();
        }

        public static string GetCurrentExecutableName()

        {
            var process = Process.GetCurrentProcess();

            if (process.MainModule == null) return string.Empty;
            try
            {
                return Path.GetFileNameWithoutExtension(process.MainModule.FileName);
            }
            catch { }

            try
            {
                return process.ProcessName;
            }
            catch { }

            return string.Empty;
        }

        public void DumpText()
        {
            if (DumpCompleted || !Enabled.Value) return;
            DumpStarted = true;
            if (IsStudio)
            {
                StartCoroutine(ExecuteDump(StudioDump()));
            }
            else
            {
                StartCoroutine(ExecuteDump(MakerDump()));
            }
        }

        private IEnumerator ExecuteDump(IEnumerator dumper)
        {
            _currentTranslationCache = GetCurrentTranslatorCache();
            yield return dumper;
            _currentTranslationCache = null;
            yield return WriteTranslations();
            DumpCompleted = true;
        }

        protected override void InitPluginSettings()
        {
            base.InitPluginSettings(PluginName, Version);
            StudioRoot = StudioRoot ?? CombinePaths(DumpRoot, "Text", "Mods", "Studio");
            MakerRoot = MakerRoot ?? CombinePaths(DumpRoot, "Text", "Mods", "Maker");
        }

        private static bool IsMod(ListInfoBase listInfo)
        {
            return IsMod(listInfo.Id);
        }

        private static bool IsMod(int id)
        {
            return id >= UniversalAutoResolver.BaseSlotID;
        }

        private IEnumerator CheckReadyToDump()
        {
            Assert.IsNotNull(TextResourceHelper, "textResourceHelper not initialized in time");

            NotificationMessage =
                $"{PluginName} will start shortly (disable in plugin settings or uninstall to disable)";

            if (IsStudio)
            {
                Logger.LogDebug($"{nameof(CheckReadyToDump)}: waiting for studio to be available");
                while (!StudioAPI.StudioLoaded || !Studio.Studio.IsInstance() || Studio.Studio.Instance == null)
                {
                    yield return CheckReadyToDumpDelay;
                }

                Logger.LogDebug($"{nameof(CheckReadyToDump)}: waiting for studio lists to be available");
                while (!Studio.Info.IsInstance() || Studio.Info.Instance == null)
                {
                    yield return CheckReadyToDumpDelay;
                }
            }
            else
            {
                Logger.LogDebug($"{nameof(CheckReadyToDump)}: waiting for character lists to be available");
                while (!Character.IsInstance() || Character.Instance == null)
                {
                    yield return CheckReadyToDumpDelay;
                }
            }

            Logger.LogDebug($"{nameof(CheckReadyToDump)}: waiting for scene to finish loading");


#if HS2
            while (Scene.initialized != true || Scene.IsNowLoading || Scene.IsNowLoadingFade)
            {
                Logger.LogFatal($"{Scene.initialized} {Scene.IsNowLoading} {Scene.IsNowLoadingFade}");
                yield return CheckReadyToDumpDelay;
            }
#else
            while (!Scene.IsInstance() || Scene.Instance == null || Scene.Instance.IsNowLoading ||
                   Scene.Instance.IsNowLoadingFade)
            {
                yield return CheckReadyToDumpDelay;
            }
#endif
            Logger.LogDebug($"{nameof(CheckReadyToDump)}: waiting for translator to be ready");
            var waiting = true;
            AutoTranslator.Default.TranslateAsync("おはよう", result => { waiting = false; });

            while (waiting)
            {
                yield return CheckReadyToDumpDelay;
            }

            Logger.LogDebug($"{nameof(CheckReadyToDump)}: wait for things to stabilize");
            yield return new WaitForSecondsRealtime(5f);

            Logger.LogDebug($"{nameof(CheckReadyToDump)}: ready to dump");
            _readyToDump = true;
        }

        private SimpleTextTranslationCache GetCurrentTranslatorCache()
        {
            try
            {
                var settingsType =
                    typeof(IPluginEnvironment).Assembly.GetType(
                        "XUnity.AutoTranslator.Plugin.Core.Configuration.Settings",
                        true);

                var autoTranslationsFilePath =
                    Traverse.Create(settingsType).Field<string>("AutoTranslationsFilePath").Value;

                return new SimpleTextTranslationCache(autoTranslationsFilePath, true, false, true);
            }
            catch
            {
                return null;
            }
        }

        private string GetMakerGrouping(ListInfoBase listInfoBase)
        {
            var fallback = "0";
            foreach (var key in MakerGroupingKeys)
            {
                var result = listInfoBase.GetInfo(key);
                if (!string.IsNullOrEmpty(result) && result != "0")
                {
                    if (result.Contains("/")) return result;
                    if (result.Length > fallback.Length) fallback = result;
                }
            }

            return fallback;
        }

        private string GetStudioCategoryName(int groupId, int categoryId)
        {
            return GetStudioCategoryName(GetStudioGroupInfo(groupId), categoryId);
        }

        private string GetStudioCategoryName(Info.GroupInfo groupInfo, int categoryId)
        {
            var result = string.Empty;
            groupInfo.SafeProc(info =>
            {
                info.dicCategory.SafeProc(dic =>
                {
                    if (dic.TryGetValue(categoryId, out var category))
                    {
#if KK
                        result = category;
#elif AI||HS2
                        result = category.name;
#endif
                    }
                });
            });
            return result;
        }

        private string GetStudioGroupName(int groupId)
        {
            var result = string.Empty;
            GetStudioGroupInfo(groupId).SafeProc(info => result = info.name);
            return result;
        }

        private Info.GroupInfo GetStudioGroupInfo(int groupId)
        {
            Info.GroupInfo result = null;
            Studio.Info.Instance.SafeProc(inst => inst.dicItemGroupCategory.SafeProc(dic =>
            {
                if (dic.TryGetValue(groupId, out var group))
                {
                    result = group;
                }
            }));
            return result;
        }

        protected override void DumpToFile(string filePath, IEnumerable<string> lines)
        {
            var dumpLines = lines;

            if (IsStudio)
            {
                var tmpLines = dumpLines.ToList();
                if (tmpLines.Count > 0)
                {
                    var tmp = new[]
                    {
                        StudioDumpPrefix.AsEnumerable(),
                        tmpLines.AsEnumerable()
                    }.SelectMany(s => s);
                    dumpLines = tmp.ToList();
                }
                else
                {
                    dumpLines = tmpLines;
                }
            }

            base.DumpToFile(filePath, dumpLines);
        }

        private IEnumerator StudioDump()
        {
            Info studioInfo = null;
            while (studioInfo == null)
            {
                yield return null;
                Studio.Info.Instance.SafeProc(i => studioInfo = i);
            }

            var groupResults = GetTranslationsForPath(CombinePaths(StudioRoot, "group_names.txt"));

            foreach (var group in studioInfo.dicItemLoadInfo)
            {
                var groupRoot = CombinePaths(StudioRoot, $"group_{group.Key:D10}");
                var categoryResults = GetTranslationsForPath(CombinePaths(groupRoot, "category_names.txt"));
                var groupInfo = GetStudioGroupInfo(group.Key);
                var groupName = groupInfo?.name ?? string.Empty;

                if (!string.IsNullOrEmpty(groupName) && ContainsNonAscii(groupName))
                {
                    var groupNameTrans = string.Empty;
                    if (TryGetTranslation(groupName, out var result)) groupNameTrans = result;
                    TextResourceHelper.AddLocalizationToResults(groupResults, groupName, groupNameTrans);
                }


                foreach (var category in group.Value)
                {
                    var results = GetTranslationsForPath(CombinePaths(groupRoot, $"category_{category.Key:D10}.txt"));
                    var categoryName = groupInfo != null
                        ? GetStudioCategoryName(groupInfo, category.Key)
                        : GetStudioCategoryName(group.Key, category.Key);

                    if (!string.IsNullOrEmpty(categoryName) && ContainsNonAscii(categoryName))
                    {
                        var categoryNameTrans = string.Empty;
                        if (TryGetTranslation(categoryName, out var result)) categoryNameTrans = result;
                        TextResourceHelper.AddLocalizationToResults(categoryResults, categoryName, categoryNameTrans);
                    }

                    foreach (var grouping in category.Value.Where(entry => IsMod(entry.Key))
                        .Select(entry => entry.Value).GroupBy(GetStudioGrouping))
                    {
                        var names = grouping.Select(i => i.name)
                            .Where(n => !results.ContainsKey(n) && ContainsNonAscii(n)).ToList();

                        if (names.Count < 1) continue;
                        TextResourceHelper.AddLocalizationToResults(results, $"{FilePattern}{grouping.Key}",
                            FilePattern);

                        foreach (var itemName in names)
                        {
                            var translation = string.Empty;
                            if (TryGetTranslation(itemName, out var result)) translation = result;
                            TextResourceHelper.AddLocalizationToResults(results, itemName, translation);
                        }
                    }
                }
            }
        }

        private string GetStudioGrouping(Info.ItemLoadInfo itemLoadInfo)
        {
            return itemLoadInfo.bundlePath;
        }

        private IEnumerable<int> GetSearchScopes()
        {
            HandledScopes.Clear();
            foreach (var scope in TranslationScopes.Where(scope => scope != -1 && !HandledScopes.Contains(scope)))
            {
                HandledScopes.Add(scope);
                yield return scope;
            }

            if (HandledScopes.Count == 0) yield return -1;
        }

        private bool TryGetTranslation(string input, out string translatedText)
        {
            translatedText = string.Empty;
            if (_currentTranslationCache == null) return false;

            string fallback = null;
            var cleanInput = _whitespaceRemover.Replace(input, string.Empty);

            foreach (var scope in GetSearchScopes())
            {
                if (!AutoTranslator.Default.TryTranslate(input, scope, out var result))
                {
                    Logger.DebugLogDebug($"{nameof(TryGetTranslation)}: No result for {input} in scope {scope}");
                    continue;
                }

                Logger.DebugLogDebug($"{nameof(TryGetTranslation)}: {input} => {result} (scope: {scope})");
                // skip if it's just whitespace, caused by some splitters
                if (cleanInput == _whitespaceRemover.Replace(result, string.Empty)) continue;

                // keep incomplete translation for last resort
                if (!result.IsRedirected() && LanguageHelper.IsTranslatable(result))
                {
                    Logger.DebugLogDebug(
                        $"{nameof(TryGetTranslation)}: {input} => {result} (scope: {scope}): partial translation");
                    if (string.IsNullOrEmpty(fallback)) fallback = result;
                    continue;
                }

                // skip any translations that match what's in the auto translation cache (unedited MTL)
                if (_currentTranslationCache.TryGetTranslation(input, true, out var mtlTranslation) &&
                    mtlTranslation == result)
                {
                    Logger.DebugLogDebug(
                        $"{nameof(TryGetTranslation)}: {input} => {result} (scope: {scope}): discarding as it's in auto-cache");
                    continue;
                }

                Logger.DebugLogDebug($"{nameof(TryGetTranslation)}: {input} => {result} (scope: {scope}): accepted");
                translatedText = result;
                return true;
            }

            if (string.IsNullOrEmpty(fallback)) return false;
            Logger.DebugLogDebug($"{nameof(TryGetTranslation)}: {input} => {fallback}: accepting fallback");
            translatedText = fallback;
            return true;
        }

        private IEnumerator MakerDump()
        {
            NotificationMessage = "Collecting Maker Mod Strings, please wait...";
            ChaListControl chaListCtrl = null;
            while (chaListCtrl == null)
            {
                yield return null;
                Character.Instance.SafeProc(i => chaListCtrl = i.chaListCtrl);
            }

            var categories = Enum.GetValues(typeof(ChaListDefine.CategoryNo)).Cast<ChaListDefine.CategoryNo>();

            foreach (var category in categories)
            {
                try
                {
                    var results = GetTranslationsForPath(CombinePaths(MakerRoot, $"{category}.txt"));
                    var categoryInfo = chaListCtrl.GetCategoryInfo(category);
                    foreach (var grouping in categoryInfo.Values.Where(IsMod).GroupBy(GetMakerGrouping))
                    {
                        try
                        {
                            var names = grouping.Select(i => i.Name)
                                .Where(n => !results.ContainsKey(n) && ContainsNonAscii(n)).ToList();

                            if (names.Count < 1) continue;

                            TextResourceHelper.AddLocalizationToResults(results, $"{FilePattern}{grouping.Key}",
                                FilePattern);
                            foreach (var itemName in names)
                            {
                                try
                                {
                                    var translation = string.Empty;
                                    if (TryGetTranslation(itemName, out var result)) translation = result;

                                    TextResourceHelper.AddLocalizationToResults(results, itemName, translation);
                                }
                                catch (Exception err)
                                {
                                    Logger.LogError($"Looping {names}: {err.Message}");
                                    UnityEngine.Debug.LogException(err);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.LogError($"Looping {grouping}: {err.Message}");
                            UnityEngine.Debug.LogException(err);
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError($"Looping {category}: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            DumpCompleted = true;
        }

        protected override List<string> CreateLines(string filePath, TranslationDictionary translations)
        {
            var lines = new List<string>();
            var scopeLines = new List<string>();
            foreach (var scope in translations.Scopes)
            {
                scopeLines.Clear();
                foreach (var localization in translations.GetScope(scope))
                {
                    var key = localization.Key;
                    var value = localization.Value;
                    value = value.IsNullOrWhiteSpace() ? string.Empty : value.FixRedirected();

                    if (key.Trim() == value.Trim()) continue;

                    if (key.StartsWith(FilePattern) && value.EndsWith(FilePattern))
                    {
                        scopeLines.Add(string.Empty);
                        scopeLines.Add($"// {key.Substring(FilePattern.Length)}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(key) || key == value ||
                        string.IsNullOrEmpty(value) && !ContainsNonAscii(key))
                    {
                        continue;
                    }

                    // comment out potential partial entries
                    if (string.IsNullOrEmpty(value) || LanguageHelper.IsTranslatable(value))
                    {
                        key = $"//{key}";
                    }

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
    }
}
