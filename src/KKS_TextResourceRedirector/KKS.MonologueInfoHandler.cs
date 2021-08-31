using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class MonologueInfoHandler : UntestedParamAssetLoadedHandler<MonologueInfo, MonologueInfo.Param>
    {
        public MonologueInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<MonologueInfo.Param> GetParams(MonologueInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            MonologueInfo.Param param)
        {
            var result = false;
            var key = param.Text;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                if (!EnableSafeMode.Value)
                {
                    WarnIfUnsafe(calculatedModificationPath);
                    param.Text = translated;
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

        public override bool DumpParam(SimpleTextTranslationCache cache, MonologueInfo.Param param)
        {
            var key = param.Text;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var val = string.Empty;
            cache.AddTranslationToCache(key, val);
            return true;
        }
    }
}
