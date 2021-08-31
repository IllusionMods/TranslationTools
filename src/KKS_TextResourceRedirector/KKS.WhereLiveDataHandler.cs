using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class WhereLiveDataHandler : UntestedParamAssetLoadedHandler<WhereLiveData, WhereLiveData.Param>
    {
        public WhereLiveDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<WhereLiveData.Param> GetParams(WhereLiveData asset)
        {
            return asset.param;
        }


        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            WhereLiveData.Param param)
        {
            var key = param.Explan;
            var result = false;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    param.Explan = translated;
                }

                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                cache.AddTranslationToCache(key, string.Empty);
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, WhereLiveData.Param param)
        {
            return DefaultDumpParam(cache, param.Explan);
        }
    }
}
