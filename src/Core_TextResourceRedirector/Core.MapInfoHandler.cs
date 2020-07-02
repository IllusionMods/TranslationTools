#if KK||HS2
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Manager;
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
    public partial class MapInfoHandler : RedirectorAssetLoadedHandlerBase<MapInfo>
    {
        internal static MapInfoHandler Instance;

        private readonly Dictionary<string, string> _mapLookup = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _reverseMapLookup = new Dictionary<string, string>();

        public MapInfoHandler(TextResourceRedirector plugin) : base(plugin, allowTranslationRegistration: true)
        {
            Instance = this;
        }

        internal int ConvertMapNameEnableCount { get; set; }

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
            Hooks.Init();

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

            // register with helper or dump without translating here
            foreach (var key in asset.param
                .Select(entry => TextResourceHelper.GetSpecializedKey(entry, GetMapName(entry)))
                .Where(k => !string.IsNullOrEmpty(k)))
            {
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    if (string.IsNullOrEmpty(translated)) continue;
                    _mapLookup[key] = translated;
                    _reverseMapLookup[translated] = key;
                    TrackReplacement(key, translated);
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

            var result = false;
            foreach (var entry in asset.param)
            {
                var key = TextResourceHelper.GetSpecializedKey(entry, GetMapName(entry));
                if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) continue;
                cache.AddTranslationToCache(key, GetMapNameTranslation(entry));
                result = true;
            }

            return result;
        }


        internal static partial class Hooks
        {
            private static bool _hooksInitialized;
            private static readonly object HookLock = new object();

            internal static void Init()
            {
                if (_hooksInitialized) return;
                lock (HookLock)
                {
                    if (_hooksInitialized) return;
                    Harmony.CreateAndPatchAll(typeof(Hooks));

                    _hooksInitialized = true;
                }
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.ConvertMapName))]
            internal static void BaseMapConvertMapNamePostfix(ref string __result)
            {
                if (Instance == null || Instance.ConvertMapNameEnableCount <= 0 ||
                    !Instance._mapLookup.TryGetValue(__result, out var translated))
                {
                    return;
                }

                Logger.DebugLogDebug($"BaseMapConvertMapNamePostfix: {__result} => {translated}");
                __result = translated;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.ConvertMapNo))]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.GetParam), typeof(string))]
            internal static void RestoreMapNamePrefix(ref string mapName)
            {
                if (Instance == null || !Instance._reverseMapLookup.TryGetValue(mapName, out var origName)) return;
                Logger.DebugLogDebug($"RestoreMapNamePrefix: {mapName} => {origName}");
                mapName = origName;
            }
        }
    }
}
#endif
