using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public abstract class StringArrayParamAssetLoadedHandler<T, TParam> : ParamAssetLoadedHandler<T, TParam>
        where T : Object
    {
        protected virtual int TranslatedIndex { get; private set; }
        protected StringArrayParamAssetLoadedHandler(TextResourceRedirector plugin, int translatedIndex = 1) :
            base(plugin)
        {
            TranslatedIndex = translatedIndex;
        }

        protected virtual bool DumpParamField(SimpleTextTranslationCache cache, string[] field)
        {
            var key = field[0];
            var value = field.Length > TranslatedIndex && string.IsNullOrEmpty(field[TranslatedIndex])
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
            var transResult = false;

            if (cache.TryGetTranslation(key, true, out var translated))
            {
                field[0] = translated;
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                transResult = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                DumpParamField(cache, field);
            }

            return transResult;
        }
    }
}
