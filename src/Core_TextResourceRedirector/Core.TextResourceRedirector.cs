using System;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        public const string Version = "1.4.4.3";
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

        private static int? _currentGameLanguage;

        protected ConfigEntry<bool> EnableTracing { get; private set; }

        internal static TextResourceRedirector Instance { get; private set; }

        internal void Awake()
        {
            Instance = this;
            Logger = Logger ?? base.Logger;

            EnableTracing = Config.Bind("Settings", "Enable Tracing", false, new ConfigDescription(
                "Enable additional low level debug log messages", null, "Advanced"));

            EnableTracing.SettingChanged += EnableTracing_SettingChanged;

            XuaHooks.Init();

            TextResourceHelper = GetTextResourceHelper();

            TextFormatter.Init(this);

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
            EnableTracing_SettingChanged(this, EventArgs.Empty);

            AdvCommandHelper.Init();
            OnTextResourceRedirectorAwake(EventArgs.Empty);
            LogTextResourceHelperSettings();
        }

        private void EnableTracing_SettingChanged(object sender, EventArgs e)
        {
            TextResourceExtensions.EnableTraces = EnableTracing.Value;
        }

        private void LogTextResourceHelperSettings()
        {
            var settings = TextResourceHelper?.GetSettingsStrings();
            if (settings == null || settings.Count == 0) return;

            var message = new StringBuilder();
            message.Append(nameof(TextResourceHelper)).Append(" (").Append(TextResourceHelper.GetType().FullName)
                .Append(") Settings:\n");
            foreach (var setting in settings.OrderBy(s => s.Key))
            {
                message.Append(" - ").Append(setting.Key).Append(":\n");
                foreach (var val in setting.Value.OrderBy(v => v))
                {
                    message.Append("    - ").Append(val).Append("\n");
                }
            }

            Logger.LogDebug(message.ToString());
        }

        protected T CreateHelper<T>() where T : IHelper
        {
            return BaseHelperFactory<T>.Create<T>();
        }

        internal void Main()
        {
            Instance = this;
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
            XuaHooks.AddTranslation(key, value, scope);
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

        public static int GetCurrentTranslationScope()
        {
            try
            {
                return Features.SupportsSceneManager
                    ? SceneManager.GetActiveScene().buildIndex
#pragma warning disable CS0618 // Type or member is obsolete - code only used in older engines
                    : Application.loadedLevel;
#pragma warning restore CS0618 // Type or member is obsolete
            }
            catch
            {
                return -1;
            }
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
