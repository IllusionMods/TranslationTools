using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class TopicTalkCommonHandler : UntestedParamAssetLoadedHandler<TopicTalkCommon, TopicTalkCommon.Param>,
        IPathListBoundHandler
    {
        public TopicTalkCommonHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<TopicTalkCommon.Param> GetParams(TopicTalkCommon asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TopicTalkCommon.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, "text");
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicTalkCommon.Param param)
        {
            return DefaultDumpParam(cache, param.text);
        }
    }
}
