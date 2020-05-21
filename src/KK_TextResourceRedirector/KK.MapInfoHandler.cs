using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionGame;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    /// <summary>
    ///     <c>MapInfo</c> assets can not be modified in place without error, but they're almost always accessed for display
    ///     via
    ///     <c>BaseMap.ConvertMapName</c>.
    ///     Postfix hook on <c>BaseMap.ConvertMapName</c> handles translation. Prefix hook on <c>BaseMap.ConvertMapNo</c>
    ///     restores
    ///     original name before doing name to id lookup.
    /// </summary>
    /// <seealso cref="XUnity.AutoTranslator.Plugin.Core.AssetRedirection.AssetLoadedHandlerBaseV2{T}" />
    /// <seealso cref="MapInfo" />
    public class MapInfoHandler : AssetLoadedHandlerBaseV2<MapInfo>
    {
        internal static MapInfoHandler Instance;

        private static bool _hooksInitialized;
        private static readonly object HookLock = new object();


        private readonly Dictionary<string, string> _mapLookup = new Dictionary<string, string>();
        private readonly TextResourceRedirector _plugin;
        private readonly Dictionary<string, string> _reverseMapLookup = new Dictionary<string, string>();

        public MapInfoHandler(TextResourceRedirector plugin)
        {
            Instance = this;
            CheckDirectory = true;
            _plugin = plugin;

            EnableRegisterAsTranslations = _plugin.Config.Bind("Map Names", "Register Assets as Translations", true,
                "If Map Names from assets should be also used for text translations");

            plugin.TranslatorTranslationsLoaded += Plugin_TranslatorTranslationsLoaded;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            Logger.LogDebug($"{GetType()} enabled");
        }

        public static ConfigEntry<bool> EnableRegisterAsTranslations { get; private set; }

        internal int ConvertMapNameEnableCount { get; set; }


        private TextResourceHelper TextResourceHelper => _plugin?.TextResourceHelper;
        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            RegisterAsTranslations();
        }

        private void Plugin_TranslatorTranslationsLoaded(TextResourceRedirector sender, EventArgs eventArgs)
        {
            RegisterAsTranslations();
        }

        private void RegisterAsTranslations()
        {
            if (!EnableRegisterAsTranslations.Value) return;
            foreach (var entry in _mapLookup)
            {
                _plugin.AddTranslationToTextCache(entry.Key, entry.Value);
            }
        }

        /// <summary>
        ///     Caches forward and reverse translation of assets as they're loaded, but does not apply them.
        ///     to avoid breaking code that relies on original names being present.
        /// </summary>
        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref MapInfo asset,
            IAssetOrResourceLoadedContext context)
        {
            // updating the MapInfo assets directly breaks places that are doing lookups by mapName
            // instead of id, so we just register this as a place to lookup MapInfo translations and 
            // return true so it appears handled
            InitHooks();

            // register new translations with helper without replacing
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            if (cache.IsEmpty) return true;

            var enableRegister = EnableRegisterAsTranslations.Value;
            // register with helper or dump without translating here
            foreach (var key in asset.param.Select(entry => TextResourceHelper.GetSpecializedKey(entry, entry.MapName))
                .Where(k => !string.IsNullOrEmpty(k)))
            {
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    if (string.IsNullOrEmpty(translated)) continue;
                    _mapLookup[key] = translated;
                    _reverseMapLookup[translated] = key;
                    if (enableRegister) _plugin.AddTranslationToTextCache(key, translated);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         !string.IsNullOrEmpty(key) && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, key);
                }
            }

            return true;
        }

        protected override bool DumpAsset(string calculatedModificationPath, MapInfo asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            foreach (var key in asset.param.Select(entry => TextResourceHelper.GetSpecializedKey(entry, entry.MapName))
                .Where(k => !string.IsNullOrEmpty(k) && LanguageHelper.IsTranslatable(k)))
            {
                cache.AddTranslationToCache(key, key);
            }

            return true;
        }

        protected override string CalculateModificationFilePath(MapInfo asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool ShouldHandleAsset(MapInfo asset, IAssetOrResourceLoadedContext context)
        {
            return !context.HasReferenceBeenRedirectedBefore(asset);
        }

        private void InitHooks()
        {
            if (_hooksInitialized) return;
            lock (HookLock)
            {
                if (_hooksInitialized) return;
                HarmonyWrapper.PatchAll(typeof(Hooks));

                _hooksInitialized = true;
            }
        }

        internal static class Hooks
        {

            [HarmonyPrefix]
            [HarmonyPatch(typeof(StaffRoomMenuScene), nameof(StaffRoomMenuScene.SetMapInfo))]
            public static void SetMapInfoPrefix(ref MapInfo.Param _mapParam)
            {
#if false
                if (Instance == null) return;

                // maybe problematic, disable for now
                var key = Instance.TextResourceHelper.GetSpecializedKey(_mapParam, _mapParam.MapName);
                if (Instance._mapLookup.TryGetValue(key, out var translated))
                {
                    Logger.LogDebug($"SetMapInfoPrefix: {_mapParam.MapName} => {translated}");
                    _mapParam.MapName = translated;
                }
#else
                Instance?.RegisterAsTranslations();
#endif
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.ConvertMapName))]
            internal static void BaseMapConvertMapNamePostfix(ref string __result)
            {
                if (Instance != null && Instance.ConvertMapNameEnableCount > 0 &&
                    Instance._mapLookup.TryGetValue(__result, out var translated))
                {
                    Logger.LogDebug($"BaseMapConvertMapNamePostfix: {__result} => {translated}");
                    __result = translated;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.ConvertMapNo))]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.GetParam), typeof(string))]
            internal static void RestoreMapNamePrefix(ref string mapName)
            {
                if (Instance != null && Instance._reverseMapLookup.TryGetValue(mapName, out var origName))
                {
                    Logger.LogDebug($"RestoreMapNamePrefix: {mapName} => {origName}");
                    mapName = origName;
                }
                
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ActionMoveUI), nameof(ActionMoveUI.Initialize))]
            [HarmonyPatch(typeof(ChaStatusScene), "Start")]
            private static void ConvertNameReplacementAllowedPrefix()
            {
                if (Instance != null) Instance.ConvertMapNameEnableCount++;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ActionMoveUI), nameof(ActionMoveUI.Initialize))]
            [HarmonyPatch(typeof(ChaStatusScene), "Start")]
            private static void ConvertNameReplacementAllowedPostfix()
            {
                if (Instance == null) return;
                Instance.ConvertMapNameEnableCount--;

                if (Instance.ConvertMapNameEnableCount < 0) Instance.ConvertMapNameEnableCount = 0;
            }
        }
    }
}
