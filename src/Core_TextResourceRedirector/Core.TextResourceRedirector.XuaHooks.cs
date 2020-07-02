using System;
using BepInEx.Harmony;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public partial class TextResourceRedirector
    {
        internal static class XuaHooks
        {
            internal static bool Initialized;

            internal static AddTranslation AddTranslationDelegate;

            internal static void Init()
            {
                if (Initialized) return;
                Initialized = true;
                Harmony.CreateAndPatchAll(typeof(XuaHooks));

                var defaultTranslator = AutoTranslator.Default;
                if (defaultTranslator == null) return;
                var defaultCache =
                    AccessTools.Field(defaultTranslator.GetType(), "TextCache")?.GetValue(defaultTranslator) ??
                    AccessTools.Property(defaultTranslator.GetType(), "TextCache")
                        ?.GetValue(defaultTranslator, new object[0]);
                if (defaultCache == null) return;
                var method = AccessTools.Method(defaultCache.GetType(), "AddTranslation");
                if (method == null) return;
                try
                {
                    AddTranslationDelegate = (AddTranslation) Delegate.CreateDelegate(
                        typeof(AddTranslation), defaultCache, method);
                }
#pragma warning disable CA1031 // non-issue in this case
                catch (ArgumentException)
                {
                    //  mono versions fallback to this
                    AddTranslationDelegate = (key, value, scope) =>
                        method.Invoke(defaultCache, new object[] {key, value, scope});
                }
#pragma warning restore CA1031
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AutoTranslationPlugin), "LoadTranslations")]
            internal static void TranslationsLoadedPostfix()
            {
                Logger.LogWarning($"{typeof(XuaHooks)} {nameof(TranslationsLoadedPostfix)} fired");
                _instance?.OnTranslatorTranslationsLoaded(EventArgs.Empty);
            }

            internal delegate void AddTranslation(string key, string value, int scope);
        }
    }
}
