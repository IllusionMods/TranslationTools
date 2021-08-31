using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class TopicTalkRareHandler : UntestedParamAssetLoadedHandler<TopicTalkRare, TopicTalkRare.Param>,
        IPathListBoundHandler
    {
        public TopicTalkRareHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<TopicTalkRare.Param> GetParams(TopicTalkRare asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TopicTalkRare.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, "text");
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicTalkRare.Param param)
        {
            return DefaultDumpParam(cache, param.text);
        }
    }
}
