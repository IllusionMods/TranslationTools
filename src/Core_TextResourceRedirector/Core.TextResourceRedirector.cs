using System;
using BepInEx;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;

#if AI
using AIChara;
#endif

//Adopted from gravydevsupreme's TextResourceRedirector
namespace IllusionMods
{
    [BepInDependency(XUnity.ResourceRedirector.Constants.PluginData.Identifier,
        XUnity.ResourceRedirector.Constants.PluginData.Version)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier,
        XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public delegate void TextResourceRedirectorAwakeHandler(TextResourceRedirector sender, EventArgs eventArgs);

        public delegate void TranslatorTranslationsLoadedHandler(TextResourceRedirector sender, EventArgs eventArgs);


        public const string PluginName = "Text Resource Redirector";
        public const string GUID = "com.deathweasel.bepinex.textresourceredirector";
        public const string Version = "1.2.3";
        internal new static ManualLogSource Logger;
#if !HS
        internal ChaListDataHandler ChaListDataHandler;
#endif

        internal ExcelDataHandler ExcelDataHandler;
        internal ScenarioDataHandler ScenarioDataHandler;

        internal TextAssetTableHandler TextAssetTableHandler;

        //internal TextAssetResourceRedirector _textAssetResourceRedirector;
        internal TextResourceHelper TextResourceHelper;
        internal TextAssetRawBytesHandler TextAssetRawBytesHandler;

        public event TextResourceRedirectorAwakeHandler TextResourceRedirectorAwake;
        public event TranslatorTranslationsLoadedHandler TranslatorTranslationsLoaded;

        private static TextResourceRedirector _instance;

        internal void Awake()
        {
            _instance = this;
            Logger = Logger ?? base.Logger;
            TextResourceHelper = GetTextResourceHelper();

            XuaHooks.Init();

            ExcelDataHandler = new ExcelDataHandler(TextResourceHelper);
            ScenarioDataHandler = new ScenarioDataHandler(TextResourceHelper);
            TextAssetTableHandler = new TextAssetTableHandler(TextResourceHelper);
            TextAssetRawBytesHandler = new TextAssetRawBytesHandler(TextResourceHelper);

            enabled = false;
#if !HS
            ChaListDataHandler = new ChaListDataHandler();
#endif
            OnTextResourceRedirectorAwake(EventArgs.Empty);
        }

        internal void Main()
        {
            _instance = this;
            Logger = Logger ?? base.Logger;
        }

        internal void OnTextResourceRedirectorAwake(EventArgs eventArgs)
        {
            TextResourceRedirectorAwake?.Invoke(this, eventArgs);
        }

        internal void OnTranslatorTranslationsLoaded(EventArgs eventArgs)
        {
            TranslatorTranslationsLoaded?.Invoke(this, eventArgs);
        }

        public void AddTranslationToTextCache(string key, string value, int scope = -1)
        {
            XuaHooks.AddTranslationDelegate?.Invoke(key, value, scope);
        }

#if false
        protected virtual bool ChaListDataTranslate(ref ChaListData chaListData, SimpleTextTranslationCache cache,
            string calculatedModificationPath)
        {
            var idx = chaListData.lstKey.IndexOf("Name");
            var result = false;
            if (idx == -1) return result;
            foreach (var entry in chaListData.dictList.Values)
            {
                if (entry.Count <= idx || !cache.TryGetTranslation(entry[idx], true, out var translation)) continue;

                TranslationHelper.RegisterRedirectedResourceTextToPath(translation, calculatedModificationPath);
                result = true;
                entry[idx] = translation;
            }

            return result;
        }
#endif
        internal static class XuaHooks
        {
            internal static bool Initialized;

            internal static AddTranslation AddTranslationDelegate;

            internal static void Init()
            {
                if (Initialized) return;
                Initialized = true;
                HarmonyWrapper.PatchAll(typeof(XuaHooks));

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
                    Logger.LogDebug("InitXUA: Primary success");
                }
                catch (ArgumentException)
                {
                    //  mono versions fallback to this
                    AddTranslationDelegate = (key, value, scope) =>
                        method.Invoke(defaultCache, new object[] {key, value, scope});
                    Logger.LogDebug("InitXUA: Secondary success");
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(AutoTranslationPlugin), "LoadTranslations")]
            internal static void TranslationsLoadedPostfix()
            {
                _instance?.OnTranslatorTranslationsLoaded(EventArgs.Empty);
            }

            internal delegate void AddTranslation(string key, string value, int scope);
        }
    }
}
