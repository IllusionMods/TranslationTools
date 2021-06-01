using System;
using BepInEx.Harmony;
using HarmonyLib;
using IllusionMods.Shared.TextDumpBase;

namespace IllusionMods
{
    public partial class TextDump
    {
        internal static class InitialDumpHook
        {
            private const string HarmonyId = "TextDump.InitialDumpHook.HarmonyID";
            private static TextDump _pluginInstance;
            private static Harmony _harmony;
            private static readonly object InitialDumpLock = new object();


            [HarmonyPrefix]
            [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string),
                typeof(string), typeof(Type), typeof(string))]
            public static void DumpBeforeInitialLoad()
            {
                if (_pluginInstance is null) return;
                lock (InitialDumpLock)
                {
                    var plugin = _pluginInstance;
                    if (plugin == null || !Enabled.Value || DumpLevelCompleted > 0) return;
                    _pluginInstance = null;
                    plugin.DumpText(nameof(DumpBeforeInitialLoad));
                    plugin.TextDumpUpdate += Unpatch;
                }
            }

            private static void Unpatch(BaseTextDumpPlugin sender, EventArgs eventArgs)
            {
                if (_harmony == null) return;
                lock (InitialDumpLock)
                {
                    var tmp = _harmony;
                    if (tmp == null) return;
                    _harmony = null;
                    tmp.UnpatchAll(HarmonyId);
                    if (sender is TextDump textDump) textDump.TextDumpUpdate -= Unpatch;
                }
            }

            internal static void Setup(TextDump textDump)
            {
                _pluginInstance = textDump;
                _harmony = Harmony.CreateAndPatchAll(typeof(InitialDumpHook), HarmonyId);
            }
        }
    }
}
