using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class TopicListenDataHandler : ParamAssetLoadedHandler<TopicListenData, TopicListenData.Param>,
        IPathListBoundHandler
    {
        public TopicListenDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<TopicListenData.Param> GetParams(TopicListenData asset)
        {
            return DefaultGetParams(asset);
        }

        private static void ApplyTranslation(string calculatedModificationPath, TopicListenData.Param param, string value)
        {
            param.text = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TopicListenData.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.text, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TopicListenData.Param param)
        {
            return DefaultDumpParam(cache, param, param.text);
        }
    }
}
