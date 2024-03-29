﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using IllusionMods.Shared;
using IllusionMods.Shared.TextDumpBase;
using JetBrains.Annotations;
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
using static Studio.Info;
using UnityDebug = UnityEngine.Debug;

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
        public const string Version = "0.7.0.1";

        private const string FilePattern = "_-_-_-_-_-_";

        private static readonly ChaListDefine.KeyType[] MakerGroupingKeys =
        {
            ChaListDefine.KeyType.MainManifest, ChaListDefine.KeyType.MainAB, ChaListDefine.KeyType.MainTexAB,
            ChaListDefine.KeyType.ThumbAB
        };

        [UsedImplicitly] private static readonly List<int> TranslationScopes = new List<int>();

        private static readonly HashSet<int> HandledScopes = new HashSet<int>();

        private static readonly string[] StudioDumpPrefix =
            {string.Empty, $"#set exe {Constants.StudioProcessName}", string.Empty};

        private readonly Regex _whitespaceRemover = new Regex(@"\s+", Constants.DefaultRegexOptions);

        private Coroutine _checkReadyCoroutine;

        private SimpleTextTranslationCache _currentTranslationCache;

        private bool _readyToDump;

        internal static ConfigEntry<DumpMode> ActiveDumpMode { get; private set; }

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
            if (!DumpStarted)
            {
                if (_checkReadyCoroutine == null)
                {
                    _checkReadyCoroutine = StartCoroutine(CheckReadyToDump());
                }

                if (_readyToDump)
                {
                    DumpText();
                }
            }

            HandleNotification();
        }

        public void DumpText()
        {
            if (DumpCompleted || !Enabled.Value) return;
            DumpStarted = true;
            NotificationMessage = $"{PluginName} in progress";
            StartCoroutine(IsStudio ? ExecuteDump(StudioDump()) : ExecuteDump(MakerDump()));
        }

        protected override void InitPluginSettings()
        {
            base.InitPluginSettings(PluginName, Version, PluginNameInternal);
            ActiveDumpMode = Config.Bind("Settings", "Dump Mode", DumpMode.TranslatorLanguageSettings,
                "Determines which strings to include in dump");
            StudioRoot = StudioRoot ?? CombinePaths(DumpRoot, "Text", "Mods", "Studio");
            MakerRoot = MakerRoot ?? CombinePaths(DumpRoot, "Text", "Mods", "Maker");
            Enabled.SettingChanged += ModTextDump_Enabled_SettingChanged;
        }

        private void ModTextDump_Enabled_SettingChanged(object sender, EventArgs e)
        {
            _readyToDump = false;
            if (_checkReadyCoroutine != null)
            {
                StopCoroutine(_checkReadyCoroutine);
                _checkReadyCoroutine = null;
            }

            // if toggling on and if this will trigger a new dump if one isn't in progress
            if (Enabled.Value && DumpStarted && DumpCompleted) DumpStarted = DumpCompleted = false;
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
                        string.IsNullOrEmpty(value) && !EntryNeedsTranslation(key))
                    {
                        continue;
                    }

                    // comment out potential partial entries
                    if (string.IsNullOrEmpty(value) || LanguageHelper.IsTranslatable(value))
                    {
                        key = $"//{key}";
                    }

                    scopeLines.Add(JoinStrings("=", Encode(key), Encode(value)));
                }

                if (scopeLines.Count <= 0) continue;
                if (lines.Count > 0) lines.Add("");
                if (scope != -1) lines.Add($"#set level {scope}");
                lines.AddRange(scopeLines);
                if (scope != -1) lines.Add($"#unset level {scope}");
            }

            return lines;
        }

        private static string Encode(string str)
        {
            return str.Replace("=", "%3D");
        }

        private static bool IsMod(LightLoadInfo lightLoadInfo)
        {
            return IsMod(lightLoadInfo.no);
        }

        private static bool IsMod(ListInfoBase listInfo)
        {
            return IsMod(listInfo.Id);
        }

        private static bool IsMod(int id)
        {
            return id >= UniversalAutoResolver.BaseSlotID;
        }


        private static bool EntryNeedsTranslation(string untranslatedText)
        {
            if (string.IsNullOrEmpty(untranslatedText)) return false;
            switch (ActiveDumpMode.Value)
            {
                case DumpMode.All:
                    return true;
                case DumpMode.AllNonAscii:
                    return ContainsNonAscii(untranslatedText);
                case DumpMode.AllNonLatin1:
                    return ContainsNonLatin1(untranslatedText);
                case DumpMode.TranslatorLanguageSettings:
                    return LanguageHelper.IsTranslatable(untranslatedText);
                default:
                    return false;
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


#if HS2 || KKS
            while (Scene.initialized != true || Scene.IsNowLoading || Scene.IsNowLoadingFade)
            {
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

        private GroupInfo GetStudioGroupInfo(Dictionary<int, GroupInfo> groupInfoDict, int groupId)
        {
            return groupInfoDict.TryGetValue(groupId, out var result) ? result : null;
        }

        private string GetStudioItemCategoryName(int groupId, int categoryId)
        {
            return GetStudioGroupCategoryName(GetStudioItemGroupInfo(groupId), categoryId);
        }

        private string GetStudioGroupCategoryName(GroupInfo groupInfo, int categoryId)
        {
            var result = string.Empty;
            groupInfo.SafeProc(info =>
            {
                info.dicCategory.SafeProc(dic =>
                {
                    if (dic.TryGetValue(categoryId, out var category))
                    {
#if KK || KKS
                        result = category;
#elif AI||HS2
                        result = category.name;
#endif
                    }
                });
            });
            return result;
        }

        private string GetStudioItemGroupName(int groupId)
        {
            var result = string.Empty;
            GetStudioItemGroupInfo(groupId).SafeProc(info => result = info.name);
            return result;
        }


        private GroupInfo GetStudioItemGroupInfo(int groupId)
        {
            GroupInfo result = null;
            Studio.Info.Instance.SafeProc(inst =>
                inst.dicItemGroupCategory.SafeProc(dic => result = GetStudioGroupInfo(dic, groupId)));

            return result;
        }

        private string GetStudioAnimeGroupName(int groupId)
        {
            var result = string.Empty;
            GetStudioAnimeGroupInfo(groupId).SafeProc(info => result = info.name);
            return result;
        }

        private GroupInfo GetStudioAnimeGroupInfo(int groupId)
        {
            GroupInfo result = null;
            Studio.Info.Instance.SafeProc(inst =>
                inst.dicAGroupCategory.SafeProc(dic => result = GetStudioGroupInfo(dic, groupId)));
            return result;
        }

        private IEnumerator StudioDump<T>(string currentRoot, string topLevelFileName, Dictionary<int, T> infoList)
            where T : LoadCommonInfo
        {
            yield return new WaitUntilStable(infoList, infoList.Count > 0 ? 1 : 3);
            var groupResults = GetTranslationsForPath(CombinePaths(currentRoot, topLevelFileName));
            foreach (var grouping in infoList.Where(e => IsMod(e.Key)).Select(e => e.Value)
                .GroupBy(GetStudioGrouping).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var names = grouping.Select(i => i.name).Where(n => !groupResults.ContainsKey(n)).Select(
                        origName =>
                        {
                            var shouldInclude = ShouldIncludeEntry(origName, out var translatedName);
                            var result = new {origName, translatedName};
                            return new {shouldInclude, result};
                        }).Where(r => r.shouldInclude && r.result.origName != r.result.translatedName)
                    .Select(r => r.result)
                    .OrderBy(n => n.origName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (names.Count < 1) continue;
                TextResourceHelper.AddLocalizationToResults(groupResults, $"{FilePattern}{grouping.Key}",
                    FilePattern);

                foreach (var entry in names)
                {
                    TextResourceHelper.AddLocalizationToResults(groupResults, entry.origName, entry.translatedName);
                }
            }
        }

        private IEnumerator WaitForStableDictionary(IDictionary dict)
        {
            yield return new WaitUntilStable(dict, dict.Count > 0 ? 1 : 3);
        }

        private IEnumerator StudioDump<T>(string currentRoot, string topLevelFileName,
            Dictionary<int, Dictionary<int, Dictionary<int, T>>> nestedInfoList,
            Dictionary<int, GroupInfo> groupInfoList)
            where T : LoadCommonInfo
        {
            var jobs = new List<Coroutine>
            {
                StartCoroutine(WaitForStableDictionary(nestedInfoList)),
                StartCoroutine(WaitForStableDictionary(groupInfoList))
            };

            foreach (var job in jobs) yield return job;

            jobs.Clear();

            var groupResults = GetTranslationsForPath(CombinePaths(currentRoot, topLevelFileName));
            foreach (var group in nestedInfoList.Select(group =>
            {
                var groupRoot = CombinePaths(currentRoot, $"group_{group.Key:D10}");
                var groupInfo = GetStudioGroupInfo(groupInfoList, group.Key);
                var groupName = groupInfo?.name ?? string.Empty;
                return new {group, groupRoot, groupInfo, groupName};
            }).OrderBy(g => g.groupRoot, StringComparer.OrdinalIgnoreCase))
            {
                var categoryResults = GetTranslationsForPath(CombinePaths(group.groupRoot, "category_names.txt"));

                if (ShouldIncludeEntry(group.groupName, out var groupNameTrans))
                {
                    TextResourceHelper.AddLocalizationToResults(groupResults, group.groupName, groupNameTrans);
                }


                foreach (var category in group.group.Value.Select(category =>
                {
                    var categoryName = GetStudioGroupCategoryName(group.groupInfo, category.Key);
                    var categoryFileName = $"category_{category.Key:D10}.txt";
                    return new {category, categoryName, categoryFileName};
                }).OrderBy(c => c.categoryFileName, StringComparer.OrdinalIgnoreCase))
                {
                    if (ShouldIncludeEntry(category.categoryName, out var categoryNameTrans))
                    {
                        TextResourceHelper.AddLocalizationToResults(categoryResults, category.categoryName,
                            categoryNameTrans);
                    }

                    jobs.Add(StartCoroutine(StudioDump(group.groupRoot, category.categoryFileName,
                        category.category.Value)));
                }
            }

            foreach (var job in jobs) yield return job;
        }

        private IEnumerator StudioDump()
        {
            Info studioInfo = null;
            while (studioInfo == null)
            {
                yield return null;
                Studio.Info.Instance.SafeProc(i => studioInfo = i);
            }

            var jobs = new List<Coroutine>
            {
                StartCoroutine(StudioDumpItems(studioInfo)),
                StartCoroutine(StudioDumpAnimations(studioInfo)),
                StartCoroutine(StudioDumpLights(studioInfo)),
                StartCoroutine(StudioDumpFilters(studioInfo)),
                StartCoroutine(StudioDumpMaps(studioInfo))
            };


            foreach (var job in jobs) yield return job;
        }

        private IEnumerator StudioDumpFilters(Info studioInfo)
        {
#if KK||KKS
            // Filters
            while (studioInfo.dicFilterLoadInfo == null) yield return null;
            yield return StartCoroutine(StudioDump(CombinePaths(StudioRoot, "filters"), "filter_names.txt",
                studioInfo.dicFilterLoadInfo));
#else
            yield break;
#endif
        }

        private IEnumerator StudioDumpMaps(Info studioInfo)
        {
            // Maps
            while (studioInfo.dicMapLoadInfo == null) yield return null;
            yield return StartCoroutine(StudioDump(CombinePaths(StudioRoot, "maps"), "map_names.txt",
                studioInfo.dicMapLoadInfo));
        }

        private IEnumerator StudioDumpLights(Info studioInfo)
        {
            // Lights
            while (studioInfo.dicLightLoadInfo == null) yield return null;
            yield return StartCoroutine(StudioDump(CombinePaths(StudioRoot, "lights"), "light_names.txt",
                studioInfo.dicLightLoadInfo));
        }

        private IEnumerator StudioDumpAnimations(Info studioInfo)
        {
            // Animations
            while (studioInfo.dicAnimeLoadInfo == null || studioInfo.dicAGroupCategory == null) yield return null;
            yield return StartCoroutine(StudioDump(CombinePaths(StudioRoot, "animations"), "group_names.txt",
                studioInfo.dicAnimeLoadInfo, studioInfo.dicAGroupCategory));
        }

        private IEnumerator StudioDumpItems(Info studioInfo)
        {
            // items 
            while (studioInfo.dicItemLoadInfo == null || studioInfo.dicItemGroupCategory == null) yield return null;
            yield return StartCoroutine(StudioDump(CombinePaths(StudioRoot, "items"), "group_names.txt",
                studioInfo.dicItemLoadInfo, studioInfo.dicItemGroupCategory));
        }

        private string GetStudioGrouping(LoadCommonInfo loadCommonInfo)
        {
            return loadCommonInfo.bundlePath;
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

        private bool ShouldIncludeEntry(string input, out string translatedText)
        {
            translatedText = string.Empty;
            if (string.IsNullOrEmpty(input)) return false;
            if (TryGetTranslation(input, out translatedText)) return input != translatedText;
            return EntryNeedsTranslation(input);
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
#if !KKS
                Character.Instance.SafeProc(i => chaListCtrl = i.chaListCtrl);
#else
                if (Character.Instance == null || !Character.Instance.isActiveAndEnabled) continue;
                chaListCtrl = Character.chaListCtrl;
#endif
            }

            var categories = Enum.GetValues(typeof(ChaListDefine.CategoryNo)).Cast<ChaListDefine.CategoryNo>();

            foreach (var category in categories)
            {
                try
                {
                    var results = GetTranslationsForPath(CombinePaths(MakerRoot, $"{category}.txt"));
                    var categoryInfo = chaListCtrl.GetCategoryInfo(category);
                    foreach (var grouping in categoryInfo.Values.Where(IsMod).GroupBy(GetMakerGrouping)
                        .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var names = grouping.Select(i => i.Name).Where(n => !results.ContainsKey(n)).Select(
                                    origName =>
                                    {
                                        var shouldInclude = ShouldIncludeEntry(origName, out var translatedName);
                                        var result = new {origName, translatedName};
                                        return new {shouldInclude, result};
                                    }).Where(r => r.shouldInclude && r.result.origName != r.result.translatedName)
                                .Select(r => r.result)
                                .OrderBy(n => n.origName, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            if (names.Count < 1) continue;

                            TextResourceHelper.AddLocalizationToResults(results, $"{FilePattern}{grouping.Key}",
                                FilePattern);
                            foreach (var entry in names)
                            {
                                TextResourceHelper.AddLocalizationToResults(results, entry.origName,
                                    entry.translatedName);
                            }
                        }
                        catch (Exception err)
                        {
                            Logger.LogError($"Looping {grouping}: {err.Message}");
                            UnityDebug.LogException(err);
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.LogError($"Looping {category}: {err.Message}");
                    UnityDebug.LogException(err);
                }
            }

            DumpCompleted = true;
        }
    }
}
