using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class TopicHandler : UntestedParamAssetLoadedHandler<Topic, Topic.Param>
    {
        public TopicHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<Topic.Param> GetParams(Topic asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Topic.Param param)
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

        public override bool DumpParam(SimpleTextTranslationCache cache, Topic.Param param)
        {
            return DefaultDumpParam(cache, param.Name);
        }
    }
}
