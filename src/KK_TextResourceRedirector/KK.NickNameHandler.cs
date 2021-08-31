using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using IllusionMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    /// <summary>
    ///     <c>NickName</c> assets can not be modified in place without error, but they can be modified after
    ///     they're all loaded.
    ///     <para>
    ///         Caches translations where replacement would normally occur, hooks specific methods used to get
    ///         names for display. Special handling needed for the page where you set the callname for a girl
    ///         in the classroom view.
    ///     </para>
    /// </summary>
    /// <seealso cref="AssetLoadedHandlerBaseV2{T}" />
    /// <seealso cref="NickName" />
    /// <seealso cref="NickNameSceneHelper" />
    public partial class NickNameHandler : RedirectorAssetLoadedHandlerBase<NickName>
    {
        private const string SupportedSceneName = "NickNameSetting";
        private static NickNameHandler Instance;

        private static bool _hooksInitialized;
        private static readonly object HookLock = new object();


        private readonly HashSet<string> _matched = new HashSet<string>();

        private readonly Dictionary<string, Dictionary<string, string>> _replacements =
            new Dictionary<string, Dictionary<string, string>>();

        public NickNameHandler(TextResourceRedirector plugin) : base(plugin, allowTranslationRegistration: true)
        {
            Instance = this;
            plugin.TranslatorTranslationsLoaded += Plugin_TranslatorTranslationsLoaded;
            InitHooks();
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }


        /// <inheritdoc />
        /// <remarks>Always returns <c>true</c> to signal nothing else should handle these.</remarks>
        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref NickName asset,
            IAssetOrResourceLoadedContext context)
        {
            var result = false;
            var start = UnityEngine.Time.realtimeSinceStartup;
            try
            {
                // updating the NickName assets directly causes issues, save off a lookup table.

                var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
                var streams =
                    HandlerHelper.GetRedirectionStreams(calculatedModificationPath, asset, context, EnableFallbackMapping);
                var cache = new SimpleTextTranslationCache(
                    defaultTranslationFile,
                    streams,
                    false,
                    true);

                if (cache.IsEmpty) return (result = true);

                var personalityKey = calculatedModificationPath
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .LastOrDefault();

                if (string.IsNullOrEmpty(personalityKey))
                {
                    return (result = true);
                }

                var replacements = _replacements.GetOrInit(personalityKey);

                foreach (var entry in asset.param)
                {
                    var key = TextResourceHelper.GetSpecializedKey(entry, entry.Name);
                    if (string.IsNullOrEmpty(key)) continue;
                    if (cache.TryGetTranslation(key, true, out var translated))
                    {
                        replacements[key] = translated;

                        // Scope 15 for the class nickname editor
                        TrackReplacement(calculatedModificationPath, entry.Name, translated, 15, -1);
                        TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                        Logger.DebugLogDebug(
                            $"{GetType().FullName}.{nameof(ReplaceOrUpdateAsset)}: {personalityKey}: {key} => {translated}");
                    }
                    else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                             LanguageHelper.IsTranslatable(key))
                    {
                        cache.AddTranslationToCache(key, entry.Name);
                    }
                }

                return (result = true);
            }
            finally
            {
                Logger.LogDebug($"{GetType()}.{nameof(ReplaceOrUpdateAsset)}: {calculatedModificationPath} => {result} ({Time.realtimeSinceStartup - start} seconds)");
            }

        }

        protected override bool DumpAsset(string calculatedModificationPath, NickName asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            foreach (var entry in asset.param)
            {
                var key = TextResourceHelper.GetSpecializedKey(entry, entry.Name);
                if (!string.IsNullOrEmpty(key) && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, entry.Name);
                }
            }

            return true;
        }

        private static string GetPersonalityKey(int personality)
        {
            return personality < 0 ? "player" : $"c{personality:00}";
        }

        private static bool TryGetReplacementsByPersonality(int personality,
            out Dictionary<string, string> replacements)
        {
            replacements = null;
            return Instance != null &&
                   Instance._replacements.TryGetValue(GetPersonalityKey(personality), out replacements);
        }

        private void SceneManager_sceneUnloaded(Scene scene)
        {
            if (scene.name != SupportedSceneName) return;
            NickNameSceneHelper.Enabled = false;
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SupportedSceneName) return;
            NickNameSceneHelper.Enabled = true;
        }

        private void Plugin_TranslatorTranslationsLoaded(TextResourceRedirector sender, EventArgs eventArgs)
        {
            _matched.Clear();
        }

        private void InitHooks()
        {
            if (_hooksInitialized) return;
            lock (HookLock)
            {
                if (_hooksInitialized) return;
                Harmony.CreateAndPatchAll(typeof(Hooks));
                _hooksInitialized = true;
            }
        }

        internal static class Hooks
        {
            // static void SaveData.CallNormalize(SaveData.CharaData charaData)
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveData), nameof(SaveData.CallNormalize))]
            internal static void SaveDataCallNormalizePostfix(ref SaveData.CharaData charaData)
            {
                var orig = charaData.callName;
                try
                {
                    if (Instance == null ||
                        Instance._matched.Contains(orig) ||
                        !TryGetReplacementsByPersonality(charaData.personality, out var replacements))
                    {
                        return;
                    }

                    var nickParam = SaveData.GetCallName(charaData);
                    var key = Instance.TextResourceHelper.GetSpecializedKey(nickParam, nickParam.Name);
                    if (!replacements.TryGetValue(key, out var translatedName)) return;
                    charaData.callName = translatedName;
                    Instance._matched.Add(translatedName);
                }
                catch (Exception err)
                {
                    Logger.LogWarning($"{nameof(SaveDataCallNormalizePostfix)}: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }


            // static SaveData.CallFileData SaveData.FindCallFileData(int personality, int id)
            [HarmonyPostfix]
            [HarmonyPatch(typeof(SaveData), nameof(SaveData.FindCallFileData), typeof(int), typeof(int))]
            internal static void SaveDataFindCallFileDataPostfix(SaveData.CallFileData __result, int personality,
                int id)
            {
                var orig = __result.name;
                try
                {
                    if (Instance == null ||
                        Instance._matched.Contains(orig) ||
                        !TryGetReplacementsByPersonality(personality, out var replacements))
                    {
                        return;
                    }

                    var nickParam = SaveData.GetCallName(personality, id);

                    var key = Instance.TextResourceHelper.GetSpecializedKey(nickParam, nickParam.Name);
                    if (!replacements.TryGetValue(key, out var translatedName)) return;
                    Traverse.Create(__result).Property<string>(nameof(__result.name)).Value = translatedName;
                }
                catch (Exception err)
                {
                    Logger.LogWarning($"{nameof(SaveDataFindCallFileDataPostfix)}: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }
    }
}
