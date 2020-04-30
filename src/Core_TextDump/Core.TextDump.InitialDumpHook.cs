using System;
using BepInEx.Harmony;
using HarmonyLib;

namespace IllusionMods
{
    public partial class TextDump
    {
        public static class InitialDumpHook
        {
            private static readonly string HarmonyId = "TextDump.InitialDumpHook.HarmonyID";
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

            private static void Unpatch(TextDump sender, EventArgs eventArgs)
            {
                if (_harmony == null) return;
                lock (InitialDumpLock)
                {
                    var tmp = _harmony;
                    if (tmp == null) return;
                    _harmony = null;
                    tmp.UnpatchAll(HarmonyId);
                    sender.TextDumpUpdate -= Unpatch;
                }
            }

            internal static void Setup(TextDump textDump)
            {
                _pluginInstance = textDump;
                _harmony = HarmonyWrapper.PatchAll(typeof(InitialDumpHook), HarmonyId);
            }
        }
    }
}
