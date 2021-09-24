using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class TopicPersonalityGroupHandler :
        ParamAssetLoadedHandler<TopicPersonalityGroup, TopicPersonalityGroup.Param>, IPathListBoundHandler
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

            foreach (var entry in param.personality)
            {
                if (DefaultUpdateParam(calculatedModificationPath, cache, param, entry, NoOpApplyParamTranslation))
                {
                    result = true;
                }
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicPersonalityGroup.Param param)
        {
            var result = false;
            foreach (var entry in param.personality)
            {
                if (DefaultDumpParam(cache, param, entry)) result = true;
            }

            return result;
        }
    }
}
