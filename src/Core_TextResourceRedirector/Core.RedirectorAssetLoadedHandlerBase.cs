using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine.SceneManagement;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;
using Object = UnityEngine.Object;
using XUAPluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;

namespace IllusionMods
{
    public abstract class RedirectorAssetLoadedHandlerBase<T> : AssetLoadedHandlerBaseV2<T>, IRedirectorHandler<T>
        where T : Object
    {
        private readonly bool _allowTranslationRegistration;
        private readonly Dictionary<string, string> _loadedReplacements = new Dictionary<string, string>();

        protected RedirectorAssetLoadedHandlerBase(TextResourceRedirector plugin, string extraEnableHelp = null,
            bool allowTranslationRegistration = false)
        {
            CheckDirectory = true;
            Plugin = plugin;
            _allowTranslationRegistration = allowTranslationRegistration;
            ConfigSectionName = GetType().Name;

            EnableHandler = this.ConfigEntryBind("Enabled", true, new ConfigDescription(
                $"Handle {typeof(T).Name} assets {extraEnableHelp ?? string.Empty}".Trim(),
                null, "Advanced"));
            if (allowTranslationRegistration)
            {
                EnableRegisterAsTranslationsHandler = this.ConfigEntryBind(
                    "Register as Translations", true, new ConfigDescription(
                        $"Register strings replaced by {ConfigSectionName} as text translations with " +
                        XUAPluginData.Name, null, "Advanced"));

#if !HS
                SceneManager.sceneLoaded += SceneManagerSceneLoadedRegisterAsTranslations;
#endif
                plugin.TranslatorTranslationsLoaded += TranslatorTranslationsLoadedRegisterAsTranslations;
            }

            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        protected ConfigEntry<bool> EnableHandler { get; }
        protected ConfigEntry<bool> EnableRegisterAsTranslationsHandler { get; }

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool EnableRegisterAsTranslations =>
            _allowTranslationRegistration && Enabled && (EnableRegisterAsTranslationsHandler?.Value ?? false);

        protected TextResourceHelper TextResourceHelper => Plugin.TextResourceHelper;

        public bool Enabled => EnableHandler.Value;

        public TextResourceRedirector Plugin { get; }

        public string ConfigSectionName { get; }

        protected void TrackReplacement(string orig, string translated)
        {
            if (!_allowTranslationRegistration || orig == translated) return;
            _loadedReplacements[orig] = translated;
            if (EnableRegisterAsTranslations) Plugin.AddTranslationToTextCache(orig, translated);
        }

        protected void RegisterAsTranslations()
        {
            if (!EnableRegisterAsTranslations) return;
            foreach (var entry in _loadedReplacements)
            {
                Plugin.AddTranslationToTextCache(entry.Key, entry.Value);
            }
        }

        protected override string CalculateModificationFilePath(T asset, IAssetOrResourceLoadedContext context)
        {
            return asset.DefaultCalculateModificationFilePath(context);
        }

        protected override bool ShouldHandleAsset(T asset, IAssetOrResourceLoadedContext context)
        {
            return this.DefaultShouldHandleAsset(asset, context);
        }

        private void TranslatorTranslationsLoadedRegisterAsTranslations(TextResourceRedirector sender,
            EventArgs eventArgs)
        {
            RegisterAsTranslations();
        }

#if !HS
        private void SceneManagerSceneLoadedRegisterAsTranslations(Scene arg0, LoadSceneMode arg1)
        {
            RegisterAsTranslations();
        }
#endif
    }
}
