using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class TopicTalkCommonHandler : ParamAssetLoadedHandler<TopicTalkCommon, TopicTalkCommon.Param>,
        IPathListBoundHandler
    {
        public TopicTalkCommonHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<TopicTalkCommon.Param> GetParams(TopicTalkCommon asset)
        {
            return DefaultGetParams(asset);
        }

        private static void ApplyTranslation(string calculatedModificationPath, TopicTalkCommon.Param param, string value)
        {
            param.text = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TopicTalkCommon.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.text, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicTalkCommon.Param param)
        {
            return DefaultDumpParam(cache, param, param.text);
        }
    }
}
