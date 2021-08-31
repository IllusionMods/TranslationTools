using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using IllusionMods.Shared;

#if !HS
using ADV;
using ADV.Commands.Base;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

#endif

namespace IllusionMods
{
    // ReSharper disable once PartialTypeWithSinglePart
    internal static partial class AdvCommandHelper
    {
        private static bool _initialized;

        private static readonly Dictionary<string, Dictionary<int, Dictionary<string, string>>> FormatDoCache =
            new Dictionary<string, Dictionary<int, Dictionary<string, string>>>();

        internal static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Harmony.CreateAndPatchAll(typeof(Hooks));

            TextResourceRedirector.Instance.TranslatorTranslationsLoaded += TranslatorTranslationsLoaded;
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
            Justification = "Depends on build target")]
        private static bool TryGetFormatDoCacheValue(string name, int scope, string key, out string value)
        {
            value = null;
            return FormatDoCache.TryGetValue(name, out var nameCache) &&
                   nameCache.TryGetValue(scope, out var scopeCache) &&
                   scopeCache.TryGetValue(key, out value);
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members",
            Justification = "Depends on build target")]
        private static void AddToFormatDoCache(string name, int scope, string key, string value)
        {
            FormatDoCache.GetOrInit(name).GetOrInit(scope)[key] = value;
        }

        private static void TranslatorTranslationsLoaded(TextResourceRedirector sender, EventArgs eventArgs)
        {
            FormatDoCache.Clear();
        }

        // ReSharper disable once PartialTypeWithSinglePart
        internal static partial class Hooks
        {
#if !HS
            [HarmonyPostfix]
            [HarmonyPatch(typeof(Format), "Do")]
            internal static void FormatDoPostfix(Format __instance, List<object> ___parameters)
            {
                if (TextResourceRedirector.Instance == null) return;
                if (!TextResourceRedirector.Instance.TextResourceHelper.FormatKeys.Contains(__instance.name)) return;
                try
                {
                    var scope = TextResourceRedirector.GetCurrentTranslationScope();
                    var orig = __instance.scenario.Vars[__instance.name].o as string;
                    if (string.IsNullOrEmpty(orig) || !LanguageHelper.IsTranslatable(orig)) return;


                    if (TryGetFormatDoCacheValue(__instance.name, scope, orig, out var cachedTrans))
                    {
                        __instance.scenario.Vars[__instance.name] = new ValData(cachedTrans);
                        return;
                    }

                    var parameters = ___parameters.Select(
                        p => p is string origP && LanguageHelper.IsTranslatable(origP) &&
                             AutoTranslator.Default.TryTranslate(origP, scope, out var transP)
                            ? transP
                            : p).ToArray();

                    var format = __instance.format;
                    format = LanguageHelper.IsTranslatable(format) &&
                             AutoTranslator.Default.TryTranslate(format, out var transFormat)
                        ? transFormat
                        : format;

                    var newResult = string.Format(format, parameters);
                    __instance.scenario.Vars[__instance.name] = new ValData(newResult);
                    if (!string.IsNullOrEmpty(orig) && !string.IsNullOrEmpty(newResult) && orig != newResult)
                    {
                        AddToFormatDoCache(__instance.name, scope, orig, newResult);
                    }
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception err)
                {
                    TextResourceRedirector.Logger?.LogWarning(
                        $"{nameof(FormatDoPostfix)}: Unexpected error: {err.Message}");
                    UnityEngine.Debug.LogException(err);
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
#endif
        }
    }
}
