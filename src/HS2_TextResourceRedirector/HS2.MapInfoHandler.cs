using HarmonyLib;
using HS2;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IllusionMods
{
    public partial class MapInfoHandler
    {
        protected static string GetMapName(MapInfo.Param mapInfoParam)
        {
            Instance.ConvertMapNameEnableCount = 1; // turn on globally
            return mapInfoParam.MapNames[0];
        }

        protected static string GetMapNameTranslation(MapInfo.Param mapInfoParam)
        {
            return (mapInfoParam.MapNames.Count > 1 ? mapInfoParam.MapNames[1] : null) ?? string.Empty;
        }


        internal static partial class Hooks
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(LobbyMainUI), "LoadMapImage")]
            internal static void LoadMapImagePostfix(LobbyMainUI __instance)
            {
                var txtMap = Traverse.Create(__instance).Field<Text>("txtMap").Value;
                if (txtMap != null && Instance._mapLookup.TryGetValue(txtMap.text, out var translation))
                {
                    txtMap.text = translation;
                }
            }

#if false
            /* disabling, causes crashes when moving between rooms from map selection menu in game */

            [HarmonyPrefix]
            [HarmonyPatch(typeof(MapSelectUI), "ResetMapScroll")]
            internal static void ResetMapScrollPrefix(MapSelectUI __instance, int _startIndex, bool _async,
                bool _forceSet)
            {
                // only want initial call with (0, false, true)
                if (_async || !_forceSet || _startIndex != 0) return;
                var scrollData = Traverse.Create(__instance).Field<MapInfo.Param[]>("scrollData").Value;
                if (scrollData == null) return;
                foreach (var entry in scrollData)
                {
                    if (!Instance._mapLookup.TryGetValue(entry.MapNames[0], out var translation)) continue;
                    entry.MapNames[0] = translation;
                }
            }
#endif
        }
    }
}
