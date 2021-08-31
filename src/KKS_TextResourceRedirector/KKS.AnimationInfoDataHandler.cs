using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class AnimationInfoDataHandler : UntestedParamAssetLoadedHandler<AnimationInfoData, AnimationInfoData.Param>
    {
        public AnimationInfoDataHandler(TextResourceRedirector plugin) :
            base(plugin, true) { }

        public override IEnumerable<AnimationInfoData.Param> GetParams(AnimationInfoData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            AnimationInfoData.Param param)
        {
            var result = false;
            var key = param.nameAnimation;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    param.nameAnimation = translated;
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

        public override bool DumpParam(SimpleTextTranslationCache cache, AnimationInfoData.Param param)
        {
            var key = param.nameAnimation;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var val = string.Empty;
            cache.AddTranslationToCache(key, val);
            return true;
        }
    }
}
