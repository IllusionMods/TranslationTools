using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Assertions;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;
using UnityObject = UnityEngine.Object;


namespace IllusionMods
{
    public abstract class ParamAssetLoadedHandler<T, TParam> : RedirectorAssetLoadedHandlerBase<T> where T : UnityObject
    {
        public delegate void ApplyParamTranslation(string calculatedModificationPath, TParam param, string value);

        private readonly HashSet<string> _warnedMembers = new HashSet<string>();

        protected ParamAssetLoadedHandler(TextResourceRedirector plugin, bool allowTranslationRegistration = false) :
            base(plugin, allowTranslationRegistration: allowTranslationRegistration) { }

        protected bool DisableEmptyCacheCheck { get; set; } = false;

        public abstract IEnumerable<TParam> GetParams(T asset);

        public abstract bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TParam param);

        public abstract bool DumpParam(SimpleTextTranslationCache cache, TParam param);

        protected void NoOpApplyParamTranslation(string calculatedModificationPath, TParam param, string value)
        {
            Assert.IsNotNull(calculatedModificationPath);
            Assert.IsTrue(param is TParam);
            Assert.IsNotNull(value);
        }

        protected List<TParam> DefaultGetParams(T asset)
        {
            return Traverse.Create(asset).Field<List<TParam>>("param")?.Value ??
                   Traverse.Create(asset).Property<List<TParam>>("param")?.Value;
        }

        protected virtual void ApplyTranslationToParam(ApplyParamTranslation applyParamTranslation,
            string calculatedModificationPath, TParam param, string value)
        {
            applyParamTranslation(calculatedModificationPath, param, value);
        }

        protected virtual bool DefaultUpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TParam param, string key, ApplyParamTranslation applyParamTranslation)
        {
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                ApplyTranslationToParam(applyParamTranslation, calculatedModificationPath, param, translated);
                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                return true;
            }

            if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                LanguageHelper.IsTranslatable(key))
            {
                DefaultDumpParam(cache, param, key);
            }

            return false;
        }

        protected virtual bool DefaultUpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TParam param, params string[] membersToUpdate)
        {
            var result = false;
            foreach (var memberName in membersToUpdate)
            {
                var field = Traverse.Create(param).Field<string>(memberName);
                if (field != null)
                {
                    void DoUpdateField(string modPath, TParam paramToUpdate, string newValue)
                    {
                        field.Value = newValue;
                    }

                    if (DefaultUpdateParam(calculatedModificationPath, cache, param, field.Value, DoUpdateField))
                    {
                        result = true;
                    }

                    continue;
                }

                var prop = Traverse.Create(param).Property<string>(memberName);
                if (prop != null)
                {
                    void DoUpdateProp(string modPath, TParam paramToUpdate, string newValue)
                    {
                        prop.Value = newValue;
                    }

                    if (DefaultUpdateParam(calculatedModificationPath, cache, param, prop.Value, DoUpdateProp))
                    {
                        result = true;
                    }

                    continue;
                }

                WarnMissingMember(memberName);
            }

            return result;
        }

        protected override bool DumpAsset(string calculatedModificationPath, T asset,
            IAssetOrResourceLoadedContext context)
        {
            var cache = GetDumpCache(calculatedModificationPath, asset, context);

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
                var cache = GetTranslationCache(calculatedModificationPath, asset, context);

                if (cache.IsEmpty && !DisableEmptyCacheCheck) return false;


                foreach (var entry in GetParams(asset))
                {
                    if (UpdateParam(calculatedModificationPath, cache, entry)) result = true;
                }

                return result;
            }
            finally
            {
                Logger.DebugLogDebug("{0}.{1}: {2} => {3} ({4} seconds)", GetType(), nameof(ReplaceOrUpdateAsset),
                    calculatedModificationPath, result, Time.realtimeSinceStartup - start);
            }
        }

        protected bool DefaultDumpParamMembers(SimpleTextTranslationCache cache, TParam param,
            params string[] membersToDump)
        {
            var result = false;
            foreach (var memberName in membersToDump)
            {
                if (!TryGetMemberValue(param, memberName, out var key))
                {
                    WarnMissingMember(memberName);
                    continue;
                }

                if (string.IsNullOrEmpty(key)) continue;
                if (DefaultDumpParam(cache, param, key)) result = true;
            }

            return result;
        }

        protected virtual bool DefaultDumpParam(SimpleTextTranslationCache cache, TParam param, string value)
        {
            var key = TextResourceHelper.GetSpecializedKey(param, value);
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(value)) return false;
            cache.AddTranslationToCache(key, string.Empty);
            return true;
        }

        // ReSharper disable once UnusedParameter.Global -- overrides might use

        protected virtual bool DefaultDumpParam(SimpleTextTranslationCache cache, TParam param, object obj,
            string value)
        {
            var key = TextResourceHelper.GetSpecializedKey(obj, value);
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(value)) return false;
            cache.AddTranslationToCache(key, string.Empty);
            return true;
        }

        private bool TryGetMemberValue(TParam param, string memberName, out string value)
        {
            value = null;
            var field = Traverse.Create(param).Field<string>(memberName);
            if (field != null)
            {
                value = field.Value;
                return true;
            }

            var prop = Traverse.Create(param).Property<string>(memberName);
            if (prop == null) return false;

            value = prop.Value;
            return true;
        }

        private void WarnMissingMember(string memberName)
        {
            if (_warnedMembers.Contains(memberName)) return;
            _warnedMembers.Add(memberName);
            Logger.LogWarning($"{GetType().Name}: Unable to access member: {typeof(TParam).FullName}.{memberName}");
        }
    }
}
