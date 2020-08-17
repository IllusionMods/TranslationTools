using System;
using BepInEx;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.ResourceRedirector.Constants;

#if AI
using AIChara;
#endif

//Adopted from gravydevsupreme's TextResourceRedirector
namespace IllusionMods
{
    [BepInDependency(PluginData.Identifier,
        PluginData.Version)]
    [BepInDependency(XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Identifier,
        XUnity.AutoTranslator.Plugin.Core.Constants.PluginData.Version)]
    public partial class TextResourceRedirector : BaseUnityPlugin
    {
        public delegate void TextResourceRedirectorAwakeHandler(TextResourceRedirector sender, EventArgs eventArgs);

        public delegate void TranslatorTranslationsLoadedHandler(TextResourceRedirector sender, EventArgs eventArgs);


        public const string PluginName = "Text Resource Redirector";
        public const string GUID = "com.deathweasel.bepinex.textresourceredirector";
        public const string Version = "1.4.1.1";
        internal new static ManualLogSource Logger;
#if !HS
        internal ChaListDataHandler ChaListDataHandler;
#endif

        internal ExcelDataHandler ExcelDataHandler;
        internal ScenarioDataHandler ScenarioDataHandler;

        internal TextAssetTableHandler TextAssetTableHandler;

        //internal TextAssetResourceRedirector _textAssetResourceRedirector;
        internal TextResourceHelper TextResourceHelper;
#if RAW_DUMP_SUPPORT
        internal TextAssetRawBytesHandler TextAssetRawBytesHandler;
#endif

        public event TextResourceRedirectorAwakeHandler TextResourceRedirectorAwake;
        public event TranslatorTranslationsLoadedHandler TranslatorTranslationsLoaded;

        private static TextResourceRedirector _instance;
        private static int? _currentGameLanguage;

        internal void Awake()
        {
            _instance = this;
            Logger = Logger ?? base.Logger;
            TextResourceHelper = GetTextResourceHelper();

            ExcelDataHandler = new ExcelDataHandler(this);
            ScenarioDataHandler = new ScenarioDataHandler(this);
            TextAssetTableHandler = new TextAssetTableHandler(this);
#if RAW_DUMP_SUPPORT
            TextAssetRawBytesHandler = new TextAssetRawBytesHandler(this);
#endif

            enabled = false;
#if !HS
            ChaListDataHandler = new ChaListDataHandler(this);
#endif
            XuaHooks.Init();
            OnTextResourceRedirectorAwake(EventArgs.Empty);
        }

        protected T CreateHelper<T>() where T : IHelper
        {
            return BaseHelperFactory<T>.Create<T>();
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

        public virtual void AddTranslationToTextCache(string key, string value, int scope = -1)
        {
            XuaHooks.AddTranslationDelegate?.Invoke(key, value, scope);
        }

        public int GetCurrentGameLanguage()
        {
            if (!_currentGameLanguage.HasValue)
            {
                _currentGameLanguage =
                    TextResourceHelper.XUnityLanguageToGameLanguage(AutoTranslatorSettings.DestinationLanguage);
            }

            return _currentGameLanguage.Value;
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

    }
}
