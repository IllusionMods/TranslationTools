using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using IllusionMods.Shared;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;
using UnityEngineObject = UnityEngine.Object;
using XUAPluginData = XUnity.AutoTranslator.Plugin.Core.Constants.PluginData;
using XUnity.AutoTranslator.Plugin.Core;

#if !HS
using UnityEngine.SceneManagement;
#endif

namespace IllusionMods
{
    public abstract class RedirectorAssetLoadedHandlerBase<T> : AssetLoadedHandlerBaseV2<T>, IRedirectorHandler<T>
        where T : UnityEngineObject
    {
        private readonly HashSet<string> _excludedTranslationRegistrationPaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<int, Dictionary<string, string>> _loadedReplacements =
            new Dictionary<int, Dictionary<string, string>>();

        protected RedirectorAssetLoadedHandlerBase(TextResourceRedirector plugin, string extraEnableHelp = null,
            bool allowTranslationRegistration = false, bool allowFallbackMapping = false)
        {
            CheckDirectory = true;
            Plugin = plugin;
            AllowTranslationRegistration = allowTranslationRegistration;
            AllowFallbackMapping = allowFallbackMapping;
            ConfigSectionName = GetType().Name;

            EnableHandler = this.ConfigEntryBind("Enabled", true, new ConfigDescription(
                $"Handle {typeof(T).Name} assets {extraEnableHelp ?? string.Empty}".Trim(),
                null, "Advanced"));
            if (allowTranslationRegistration)
            {
                EnableRegisterAsTranslationsHandler = this.ConfigEntryBind(
                    "Register as Translations", true, new ConfigDescription(
                        $"Register strings replaced by {ConfigSectionName} as text translations with {XUAPluginData.Name}",
                        null, "Advanced"));

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

        protected ConfigEntry<bool> EnableHandler { get; }
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

        public ManualLogSource GetLogger() => Logger;

        public void ExcludePathFromTranslationRegistration(string path)
        {
            _excludedTranslationRegistrationPaths.Add(path);
        }

        public bool IsTranslationRegistrationAllowed(string path)
        {
            return AllowTranslationRegistration && !_excludedTranslationRegistrationPaths.Contains(path);
        }

        public bool IsRandomNameListAsset(string assetName)
        {
            return TextResourceHelper.IsRandomNameListAsset(assetName);
        }

        protected virtual void TrackReplacement(string calculatedModificationPath, string orig, string translated,
            HashSet<int> scopes)
        {
            if (orig == translated || !IsTranslationRegistrationAllowed(calculatedModificationPath)) return;

            if (scopes == null) scopes = new HashSet<int> {-1};
            if (scopes.Count == 0) scopes.Add(-1);

            Logger.DebugLogDebug("{0}.{1}: {2}: {3} => {4}", GetType().Name, nameof(TrackReplacement),
                calculatedModificationPath, orig, translated);

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

        protected override string CalculateModificationFilePath(T asset, IAssetOrResourceLoadedContext context)
        {
            var path = asset.DefaultCalculateModificationFilePath(context);
            if (AllowTranslationRegistration && TextResourceHelper.IsRandomNameListAsset(asset.name))
            {
                ExcludePathFromTranslationRegistration(path);
            }

            return path;
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

        public virtual bool ShouldHandleAssetForContext(T asset, IAssetOrResourceLoadedContext context)
        {
            return !context.HasReferenceBeenRedirectedBefore(asset);
        }

        protected SimpleTextTranslationCache GetDumpCache(string calculatedModificationPath, T asset,
            IAssetOrResourceLoadedContext context)
        {
            return this.GetDumpCache<T>(calculatedModificationPath, asset, context);
        }


        protected SimpleTextTranslationCache GetTranslationCache(string calculatedModificationPath, T asset,
            IAssetOrResourceLoadedContext context)
        {
            return this.GetTranslationCache<T>(calculatedModificationPath, asset, context);
        }


#if !HS
        private void SceneManagerSceneLoadedRegisterAsTranslations(Scene arg0, LoadSceneMode arg1)
        {
            RegisterAsTranslations();
        }


#endif
    }
}
