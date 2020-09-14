using System.Collections.Generic;
using HarmonyLib;

namespace IllusionMods
{
    public partial class RandomNameProvider
    {
        public static partial class Hooks
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadExcelData))]
            public static void GetExcelDataPrefix(string assetBunndlePath, string assetName, ref LoadOptions __state)
            {
                __state = IsRandomNameAsset(assetBunndlePath, assetName) ? GetLoadOptions() : LoadOptions.None;
            }


            [HarmonyPostfix]
            [HarmonyPatch(typeof(ChaListControl), nameof(ChaListControl.LoadExcelData))]
            public static void GetExcelDataPostfix(ref List<ExcelData.Param> __result, LoadOptions __state)
            {
                if (__state.HasFlag(LoadOptions.Dump))
                {
                    Logger.LogDebug("Dumping Default Names");
                    DumpNamesToFile(__result);
                }

                if (!__state.HasFlag(LoadOptions.LoadNames)) return;

                var names = LoadNames();
                if (names == null || names.Count < 1)
                {
                    Logger.LogWarning("No names loaded, using default values");
                    return;
                }

                if (__state.HasFlag(LoadOptions.Replace))
                {
                    Logger.LogDebug("Replacing Names");
                    __result = names;
                }
                else
                {
                    Logger.LogDebug("Appending Names");
                    __result.AddRange(names);
                }
            }
        }
    }
}
