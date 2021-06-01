using System;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace IllusionMods
{
    // ReSharper disable PartialTypeWithSinglePart
    internal static partial class TextFormatter
    {
        internal static readonly string[] ParagraphBreak = {"\n\n"};
        internal static bool Initialized;
        [UsedImplicitly]
        internal static ConfigEntry<bool> EnableTextReflow { get; private set; }

        internal static void Init(TextResourceRedirector plugin)
        {
            if (Initialized) return;
            Initialized = true;
            EnableTextReflow = plugin.Config.Bind("Text Formatter", "Alternate Text Reflow", true,
                "Use whitespace based text reflow for non-Japanese languages.");
            TextResourceRedirector.Logger.LogDebug($"Hooking {typeof(Hooks).FullName}");
            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        internal static string ReflowText(string input, bool preserveParagraphBreaks = true)
        {
            if (input.IsNullOrWhiteSpace()) return input;

            if (preserveParagraphBreaks)
            {
                return string.Join(ParagraphBreak[0],
                    input.Split(ParagraphBreak, StringSplitOptions.RemoveEmptyEntries).Select(ReflowParagraph)
                        .ToArray());
            }

            return ReflowParagraph(input);
        }

        private static string ReflowParagraph(string input)
        {
            return input.IsNullOrWhiteSpace()
                ? input
                : string.Join(" ", input.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries));
        }

        internal static partial class Hooks
        {
#if KK||AI||HS2

            [HarmonyPrefix]
            [HarmonyPatch(typeof(HyphenationJpn), "GetFormatedText")]
            public static bool GetFormatedTextPrefix(Text textComp, string msg, ref string __result)
            {
                if (msg.IsNullOrEmpty() || !EnableTextReflow.Value) return true;
                try
                {
                    textComp.horizontalOverflow = HorizontalWrapMode.Wrap;
                    __result = ReflowText(msg);
                    return false;
                }
#pragma warning disable CA1031
                catch (Exception err)
                {
                    TextResourceRedirector.Logger.LogWarning(
                        $"{nameof(GetFormatedTextPrefix)}: Unexpected error: {err.Message}");
                    TextResourceRedirector.Logger.LogDebug(err);
                    return true;
                }
#pragma warning restore CA1031
            }

#endif
        }
    }
}
