using GameSetup;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;
using System;

#if false
namespace IllusionMods
{
    public class MapInfoHandler : UntestedParamAssetLoadedHandler<MapInfo, MapInfo.Param>
    {
        public MapInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<MapInfo.Param> GetParams(MapInfo asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            MapInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, "DisplayName", "MapName");
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, MapInfo.Param param)
        {
            return DefaultDumpParam(cache, param, "DisplayName", "MapName");
        }
    }
}

#else
namespace IllusionMods
{
    /// <summary>
    ///     <c>MapInfo</c> assets can not be modified in place without error, but they're almost always accessed for display
    ///     via <c>BaseMap.ConvertMapName</c>. Postfix hook on <c>BaseMap.ConvertMapName</c> handles translation. Prefix hook
    ///     on <c>BaseMap.ConvertMapNo</c> restores original name before doing name to id lookup.
    /// </summary>
    /// <seealso cref="XUnity.AutoTranslator.Plugin.Core.AssetRedirection.AssetLoadedHandlerBaseV2{T}" />
    /// <seealso cref="MapInfo" />
    public partial class MapInfoHandler 
    {
        private readonly Dictionary<string, string> _displayNameLookup = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _reverseDisplayNameLookup = new Dictionary<string, string>();

        protected static string GetMapName(MapInfo.Param mapInfoParam)
        {
            return mapInfoParam.MapName;
        }

        protected static string GetMapDisplayName(MapInfo.Param mapInfoParam)
        {
            return mapInfoParam.DisplayName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        protected static string GetMapNameTranslation(MapInfo.Param mapInfoParam)
        {
            return string.Empty;
        }

        private void GameSpecificReplaceOrUpdateAsset(string calculatedModificationPath, ref MapInfo asset, IAssetOrResourceLoadedContext context, SimpleTextTranslationCache cache, bool shouldTrack)
        {
            foreach (var key in asset.param.Select(GetMapDisplayName).Where(k => !string.IsNullOrEmpty(k)))
            {
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    if (string.IsNullOrEmpty(translated)) continue;
                    _displayNameLookup[key] = translated;
                    _reverseDisplayNameLookup[translated] = key;
                    if (shouldTrack) TrackReplacement(calculatedModificationPath, key, translated);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         !string.IsNullOrEmpty(key) && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, key);
                }
            }
            
        }

        internal static partial class Hooks
        {
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaseMap), nameof(BaseMap.ConvertDisplayName))]
            internal static void BaseMapConvertDisplayNamePostfix(ref string __result)
            {
                try
                {
                    if (Instance == null || Instance.ConvertMapNameEnableCount <= 0 ||
                        !Instance._displayNameLookup.TryGetValue(__result, out var translated)) return;
                    Logger.DebugLogDebug("{0}: {1} => {2}", nameof(BaseMapConvertDisplayNamePostfix), __result,
                        translated);
                    __result = translated;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(BaseMapConvertDisplayNamePostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HeroineStatusComponent.Page1), nameof(HeroineStatusComponent.Page1.SetMap),
                typeof(string))]
            private static void Page1SetMapPrefix(ref string text)
            {
                try
                {
                    if (Instance == null) return;
                    if (Instance._displayNameLookup.TryGetValue(text, out var translated))
                    {
                        text = translated;
                    }
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(Page1SetMapPrefix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }


            [HarmonyPrefix]
            [HarmonyPatch(typeof(HeroineStatusScene), nameof(HeroineStatusScene.SetHeroineStatus))]
            [HarmonyPatch(typeof(PlayerStatusScene), nameof(PlayerStatusScene.Setup))]
            [HarmonyPatch(typeof(TalkScene.ADVParam), nameof(TalkScene.ADVParam.Init))]
            [HarmonyPatch(typeof(ActionGame.ActionMoveUI), nameof(ActionGame.ActionMoveUI.Initialize))]
            private static void ConvertNameReplacementAllowedPrefix()
            {
                try
                {
                    if (Instance == null) return;
                    Instance.ConvertMapNameEnableCount++;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(ConvertNameReplacementAllowedPrefix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HeroineStatusScene), nameof(HeroineStatusScene.SetHeroineStatus))]
            [HarmonyPatch(typeof(PlayerStatusScene), nameof(PlayerStatusScene.Setup))]
            [HarmonyPatch(typeof(TalkScene.ADVParam), nameof(TalkScene.ADVParam.Init))]
            [HarmonyPatch(typeof(ActionGame.ActionMoveUI), nameof(ActionGame.ActionMoveUI.Initialize))]
            private static void ConvertNameReplacementAllowedPostfix()
            {
                try
                {
                    if (Instance == null) return;
                    Instance.ConvertMapNameEnableCount--;

                    if (Instance.ConvertMapNameEnableCount < 0) Instance.ConvertMapNameEnableCount = 0;
                }
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(ConvertNameReplacementAllowedPostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
            }
        }
    }
}
#endif
