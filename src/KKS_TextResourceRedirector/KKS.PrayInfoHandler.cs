using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class PrayInfoHandler : UntestedParamAssetLoadedHandler<PrayInfo,PrayInfo.Param>
    {
        public PrayInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }
        public override IEnumerable<PrayInfo.Param> GetParams(PrayInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache, PrayInfo.Param param)
        {
            var nameResult = UpdateName(calculatedModificationPath, cache, param);
            var explanResult = UpdateExplan(calculatedModificationPath, cache, param);
            return nameResult || explanResult;
        }

        private bool UpdateExplan(string calculatedModificationPath, SimpleTextTranslationCache cache,
            PrayInfo.Param param)
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

        private bool UpdateName(string calculatedModificationPath, SimpleTextTranslationCache cache, PrayInfo.Param param)
        {
            var result = false;
            var key = param.Name;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    param.Name = translated;
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
        public override bool DumpParam(SimpleTextTranslationCache cache, PrayInfo.Param param)
        {
            var nameResult = DefaultDumpParam(cache, param.Name);
            var explanResult = DefaultDumpParam(cache, param.Explan);
            return nameResult || explanResult;
        }
    }
}
