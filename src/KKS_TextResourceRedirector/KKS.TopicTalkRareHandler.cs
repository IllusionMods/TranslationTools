using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class TopicTalkRareHandler : ParamAssetLoadedHandler<TopicTalkRare, TopicTalkRare.Param>,
        IPathListBoundHandler
    {
        public TopicTalkRareHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<TopicTalkRare.Param> GetParams(TopicTalkRare asset)
        {
            return DefaultGetParams(asset);
        }

        private static void ApplyTranslation(string calculatedModificationPath, TopicTalkRare.Param param, string value)
        {
            param.text = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TopicTalkRare.Param param)
        {

            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.text, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicTalkRare.Param param)
        {
            return DefaultDumpParam(cache, param, param.text);
        }
    }
}
