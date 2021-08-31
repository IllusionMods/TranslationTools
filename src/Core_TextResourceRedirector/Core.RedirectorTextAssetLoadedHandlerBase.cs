using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using IllusionMods.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;
using XUAPluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;

namespace IllusionMods
{
    public abstract class RedirectorTextAssetLoadedHandlerBase : TextAssetLoadedHandlerBase,
        IRedirectorHandler<TextAsset>
    {
        private readonly HashSet<string> _excludedTranslationRegistrationPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<int, Dictionary<string, string>> _loadedReplacements =
            new Dictionary<int, Dictionary<string, string>>();

        protected RedirectorTextAssetLoadedHandlerBase(TextResourceRedirector plugin, string extraEnableHelp = null,
            bool allowTranslationRegistration = false, bool allowFallbackMapping = false)
        {
            CheckDirectory = true;
            AllowTranslationRegistration = allowTranslationRegistration;
            AllowFallbackMapping = allowFallbackMapping;
            Plugin = plugin;
            ConfigSectionName = GetType().Name;

            EnableHandler = this.ConfigEntryBind("Enabled", true, new ConfigDescription(
                $"Handle {nameof(TextAsset)} assets {extraEnableHelp ?? string.Empty}".Trim(),
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

            if (allowFallbackMapping)
            {
                EnableFallbackMappingConfig = this.ConfigEntryBind("Allow fallback mapping",
                    TextResourceRedirector.EnableFallbackMappingConfigDefault, new ConfigDescription(
                        $@"
Allow searching related assets for otherwise unhandled {ConfigSectionName} translations.
May slow down load times at cost of improved translations
(especially useful when game has new content).".ToSingleLineString(), null, "Advanced"));
            }

            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        public ConfigEntry<bool> EnableHandler { get; }

        protected ConfigEntry<bool> EnableRegisterAsTranslationsHandler { get; }
        protected ConfigEntry<bool> EnableFallbackMappingConfig { get;  }

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool EnableRegisterAsTranslations =>
            AllowTranslationRegistration && Enabled && (EnableRegisterAsTranslationsHandler?.Value ?? false);
        public bool EnableFallbackMapping =>
            AllowFallbackMapping && Enabled && (EnableFallbackMappingConfig?.Value ?? false) &&
            TextResourceRedirector.Instance.TextResourceHelper.ResourceMappingHelper.HasReplacementMappingsForCurrentGameMode;

        protected TextResourceHelper TextResourceHelper => Plugin.TextResourceHelper;

        public bool Enabled => EnableHandler.Value;
        public TextResourceRedirector Plugin { get; }

        public string ConfigSectionName { get; }

        public bool AllowTranslationRegistration { get; }
        public bool AllowFallbackMapping { get; }

        public void ExcludePathFromTranslationRegistration(string path)
        {
            _excludedTranslationRegistrationPaths.Add(path);
        }

        public bool IsTranslationRegistrationAllowed(string path)
        {
            return AllowTranslationRegistration && !_excludedTranslationRegistrationPaths.Contains(path);
        }


        protected virtual void TrackReplacement(string calculatedModificationPath, string orig, string translated,
            HashSet<int> scopes)
        {
            if (orig == translated || !IsTranslationRegistrationAllowed(calculatedModificationPath)) return;

            if (scopes == null) scopes = new HashSet<int> {-1};
            if (scopes.Count == 0) scopes.Add(-1);
        
            var enableRegisterAsTranslations = EnableRegisterAsTranslations;
            foreach (var scope in scopes)
            {
                _loadedReplacements.GetOrInit(scope)[orig] = translated;
                if (enableRegisterAsTranslations) Plugin.AddTranslationToTextCache(orig, translated, scope);
            }
        }

        protected void TrackReplacement(string calculatedModificationPath, string orig, string translated,
            IEnumerable<int> scopes)
        {
            TrackReplacement(calculatedModificationPath, orig, translated, new HashSet<int>(scopes));
        }

        protected void TrackReplacement(string calculatedModificationPath, string orig, string translated,
            params int[] scopes)
        {
            TrackReplacement(calculatedModificationPath, orig, translated, new HashSet<int>(scopes));
        }

        protected void RegisterAsTranslations()
        {
            if (!EnableRegisterAsTranslations) return;
            foreach (var scopeEntry in _loadedReplacements)
            {
                foreach (var entry in scopeEntry.Value)
                {
                    Plugin.AddTranslationToTextCache(entry.Key, entry.Value, scopeEntry.Key);
                }
            }
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            var path = asset.DefaultCalculateModificationFilePath(context);
            if (AllowTranslationRegistration && TextResourceHelper.IsRandomNameListAsset(asset.name))
            {
                ExcludePathFromTranslationRegistration(path);
            }

            return path;
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return this.DefaultShouldHandleAsset(asset, context);
        }

        private void TranslatorTranslationsLoadedRegisterAsTranslations(TextResourceRedirector sender,
            EventArgs eventArgs)
        {
            RegisterAsTranslations();
        }

        private void SceneManagerSceneLoadedRegisterAsTranslations(Scene arg0, LoadSceneMode arg1)
        {
            RegisterAsTranslations();
        }
    }
}
