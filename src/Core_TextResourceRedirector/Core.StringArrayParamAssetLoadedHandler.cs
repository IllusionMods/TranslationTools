using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using BepInEx.Configuration;

namespace IllusionMods
{
    public abstract class StringArrayParamAssetLoadedHandler<T, TParam> : ParamAssetLoadedHandler<T, TParam>
        where T : Object
    {

        public ConfigEntry<bool> EnableInternalAssetTranslation;
        protected StringArrayParamAssetLoadedHandler(TextResourceRedirector plugin, int translatedIndex = -1,
            bool allowTranslationRegistration = true) :
            base(plugin, allowTranslationRegistration)
        {
            EnableInternalAssetTranslation = this.ConfigEntryBind("Use Translation from Asset", true,
                $"Use translation stored in {typeof(T).Name} assets if possible");

            if (translatedIndex == -1) translatedIndex = Plugin.GetCurrentGameLanguage();
            if (translatedIndex > 0) DisableEmptyCacheCheck = true;
            TranslatedIndex = translatedIndex;
        }

        protected virtual int TranslatedIndex { get; }

        protected virtual bool DumpParamField(SimpleTextTranslationCache cache, string[] field)
        {
            var key = field[0];
            var value = TranslatedIndex > 0 && field.Length > TranslatedIndex && string.IsNullOrEmpty(field[TranslatedIndex])
                ? field[TranslatedIndex]
                : key;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            cache.AddTranslationToCache(key, value);
            return true;
        }

        protected virtual bool UpdateParamField(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ref string[] field)
        {
            var key = field[0];
            if (string.IsNullOrEmpty(key)) return false;

            if (cache.TryGetTranslation(key, true, out var translated))
            {
                field[0] = translated;

                TrackReplacement(key, translated);

                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                return true;
            }

            if (EnableInternalAssetTranslation.Value && TranslatedIndex > 0 && field.Length > TranslatedIndex)
            {
                var possible = field[TranslatedIndex];
                if (Plugin.TextResourceHelper.IsValidStringArrayParamAssetTranslation(key, possible))
                {
                    field[0] = possible;
                    TrackReplacement(key, possible);

                    TranslationHelper.RegisterRedirectedResourceTextToPath(possible, calculatedModificationPath + " (original asset)");
                    return true;
                }
            }

            if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                LanguageHelper.IsTranslatable(key))
            {
                DumpParamField(cache, field);
            }

            return false;
        }
    }
}
