using System;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public partial class TextResourceRedirector
    {
        internal static class XuaHooks
        {
            internal static bool Initialized;

            private static AddTranslationDelegate _addTranslationDelegate;

            internal static void AddTranslation(string key, string value, int scope)
            {
                _addTranslationDelegate?.Invoke(key, value, scope);
            }

            internal static void Init()
            {
                if (Initialized) return;
                Initialized = true;
                Harmony.CreateAndPatchAll(typeof(XuaHooks));

                var defaultTranslator = AutoTranslator.Default;
                if (defaultTranslator == null)
                {
                    Logger.LogWarning(
                        $"{typeof(XuaHooks).FullName}.{nameof(Init)}: Unable to access {typeof(AutoTranslator).FullName}, some functionality will be disabled");
                    return;
                }

                var defaultCache =
                    AccessTools.Field(defaultTranslator.GetType(), "TextCache")?.GetValue(defaultTranslator) ??
                    AccessTools.Property(defaultTranslator.GetType(), "TextCache")
                        ?.GetValue(defaultTranslator, new object[0]);
                if (defaultCache == null)
                {
                    Logger.LogWarning(
                        $"{typeof(XuaHooks).FullName}.{nameof(Init)}: Unable to access {defaultTranslator.GetType().FullName}'s translation cache, some functionality will be disabled");
                    return;
                }

                var method = AccessTools.Method(defaultCache.GetType(), "AddTranslation");
                if (method == null)
                {
                    Logger.LogWarning(
                        $"{typeof(XuaHooks).FullName}.{nameof(Init)}: Unable to add entries to {defaultCache.GetType().FullName}'s translation cache, some functionality will be disabled");
                    return;
                }

                try
                {
                    _addTranslationDelegate = (AddTranslationDelegate) Delegate.CreateDelegate(
                        typeof(AddTranslationDelegate), defaultCache, method);
                }
                catch (ArgumentException)
                {
                    //  mono versions fallback to this
                    _addTranslationDelegate = (key, value, scope) =>
                        method.Invoke(defaultCache, new object[] {key, value, scope});
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AutoTranslationPlugin), "LoadTranslations")]
            internal static void TranslationsLoadedPostfix()
            {
                Instance.SafeProc(i => i.OnTranslatorTranslationsLoaded(EventArgs.Empty));
            }

            internal delegate void AddTranslationDelegate(string key, string value, int scope);
        }
    }
}
