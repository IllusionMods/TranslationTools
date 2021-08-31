using ActionGame;
using HarmonyLib;
using JetBrains.Annotations;

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
        protected static string GetMapName(MapInfo.Param mapInfoParam)
        {
            return mapInfoParam.MapName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        protected static string GetMapNameTranslation(MapInfo.Param mapInfoParam)
        {
            return string.Empty;
        }


        internal static partial class Hooks
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
