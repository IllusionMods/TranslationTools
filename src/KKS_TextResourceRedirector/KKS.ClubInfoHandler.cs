using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class ClubInfoHandler : UntestedParamAssetLoadedHandler<ClubInfo, ClubInfo.Param>
    {
        public ClubInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<ClubInfo.Param> GetParams(ClubInfo asset)
        {
            return asset.param;
        }


        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClubInfo.Param param)
        {
            var nameResult = UpdateName(calculatedModificationPath, cache, param);
            var placeResult = UpdatePlace(calculatedModificationPath, cache, param);
            return nameResult || placeResult;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, ClubInfo.Param param)
        {
            var nameResult = DefaultDumpParam(cache, param.Name);
            var placeResult = DefaultDumpParam(cache, param.Place);
            return nameResult || placeResult;
        }

        private bool UpdatePlace(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClubInfo.Param param)
        {
            var key = param.Place;
            var result = false;
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

        private bool UpdateName(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ClubInfo.Param param)
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
    }
}
