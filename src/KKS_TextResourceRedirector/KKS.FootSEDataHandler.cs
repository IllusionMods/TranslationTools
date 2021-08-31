using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class FootSEDataHandler : UntestedParamAssetLoadedHandler<FootSEData, FootSEData.Param>
    {
        public FootSEDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<FootSEData.Param> GetParams(FootSEData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            FootSEData.Param param)
        {
            var result = false;
            var key = param.supplement;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    param.supplement = translated;
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

        public override bool DumpParam(SimpleTextTranslationCache cache, FootSEData.Param param)
        {
            return DefaultDumpParam(cache, param.supplement);
        }
    }
}
