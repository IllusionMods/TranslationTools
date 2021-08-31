using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class TopicPersonalityGroupHandler :
        UntestedParamAssetLoadedHandler<TopicPersonalityGroup, TopicPersonalityGroup.Param>, IPathListBoundHandler
    {
        public TopicPersonalityGroupHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<TopicPersonalityGroup.Param> GetParams(TopicPersonalityGroup asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TopicPersonalityGroup.Param param)
        {
            var result = false;

            for (var i = 0; i < param.personality.Length; i++)
            {
                var key = param.personality[i];
                if (string.IsNullOrEmpty(key)) return false;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    if (!EnableSafeMode.Value)
                    {
                        WarnIfUnsafe(calculatedModificationPath);
                        param.personality[i] = translated;
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
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicPersonalityGroup.Param param)
        {
            var result = false;
            foreach (var entry in param.personality)
            {
                if (DefaultDumpParam(cache, entry)) result = true;
            }

            return result;
        }
    }
}
