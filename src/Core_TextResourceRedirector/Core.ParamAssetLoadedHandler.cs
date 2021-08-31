using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public abstract class ParamAssetLoadedHandler<T, TParam> : RedirectorAssetLoadedHandlerBase<T> where T : Object
    {
        protected ParamAssetLoadedHandler(TextResourceRedirector plugin, bool allowTranslationRegistration = false) :
            base(plugin, allowTranslationRegistration: allowTranslationRegistration) { }

        protected bool DisableEmptyCacheCheck { get; set; } = false;

        public abstract IEnumerable<TParam> GetParams(T asset);

        public abstract bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TParam param);

        public abstract bool DumpParam(SimpleTextTranslationCache cache, TParam param);

        private readonly HashSet<string> _warnedFields = new HashSet<string>();
        protected List<TParam> DefaultGetParams(T asset)
        {
            return Traverse.Create(asset).Field<List<TParam>>("param")?.Value ??
                   Traverse.Create(asset).Property<List<TParam>>("param")?.Value;
        }

        protected virtual bool DefaultUpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TParam param, params string[] fieldsToUpdate)
        {
            var result = false;
            foreach (var fieldName in fieldsToUpdate)
            {
                var field = Traverse.Create(param).Field<string>(fieldName);
                if (field == null)
                {
                    WarnMissingField(fieldName);
                    continue;
                }

                var key = field.Value;
                if (string.IsNullOrEmpty(key)) continue;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    DefaultUpdateParamValue(calculatedModificationPath, field, translated);

                    TrackReplacement(calculatedModificationPath, key, translated);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                    result = true;
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, string.Empty);
                }
            }

            return result;
        }

        protected virtual void DefaultUpdateParamValue(string calculatedModificationPath, Traverse<string> field, string translated)
        {
            field.Value = translated;
        }

        private void WarnMissingField(string fieldName)
        {
            if (_warnedFields.Contains(fieldName)) return;
            _warnedFields.Add(fieldName);
            Logger.LogWarning($"{GetType().Name}: Unable to access field '{fieldName}' on type {typeof(TParam).Name}");
        }

        protected override bool DumpAsset(string calculatedModificationPath, T asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            var result = false;
            foreach (var entry in GetParams(asset))
            {
                if (DumpParam(cache, entry)) result = true;
            }

            return result;
        }

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref T asset,
            IAssetOrResourceLoadedContext context)
        {
            var result = false;
            var start = Time.realtimeSinceStartup;
            try
            {
                var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
                var streams =
                    HandlerHelper.GetRedirectionStreams(calculatedModificationPath, asset, context,
                        EnableFallbackMapping);
                var cache = new SimpleTextTranslationCache(
                    defaultTranslationFile,
                    streams,
                    false,
                    true);

                if (cache.IsEmpty && !DisableEmptyCacheCheck) return false;


                foreach (var entry in GetParams(asset))
                {
                    if (UpdateParam(calculatedModificationPath, cache, entry)) result = true;
                }

                return result;
            }
            finally
            {
                Logger.LogDebug(
                    $"{GetType()}.{nameof(ReplaceOrUpdateAsset)}: {calculatedModificationPath} => {result} ({Time.realtimeSinceStartup - start} seconds)");
            }
        }

        protected bool DefaultDumpParam(SimpleTextTranslationCache cache, TParam param, params string[] fieldsToDump)
        {
            var result = false;
            foreach (var fieldName in fieldsToDump)
            {
                var field = Traverse.Create(param).Field<string>(fieldName);
                if (field == null)
                {
                    WarnMissingField(fieldName);
                    continue;
                }

                var key = field.Value;
                if (string.IsNullOrEmpty(key)) continue;
                if (DefaultDumpParam(cache, key)) result = true;
            }

            return result;
        }

        protected bool DefaultDumpParam(SimpleTextTranslationCache cache, string key)
        {
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var val = string.Empty;
            cache.AddTranslationToCache(key, val);
            return true;
        }
    }
}
