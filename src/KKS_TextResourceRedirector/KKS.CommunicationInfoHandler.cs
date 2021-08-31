using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class CommunicationInfoHandler : UntestedParamAssetLoadedHandler<CommunicationInfo, CommunicationInfo.Param>,
        IPathListBoundHandler
    {
        public CommunicationInfoHandler(TextResourceRedirector plugin) :
            base(plugin, true, false) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<CommunicationInfo.Param> GetParams(CommunicationInfo asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            CommunicationInfo.Param param)
        {
            // the multi-quoted string entries need some work, skip them
            if (param.text.Contains("」「")) return false;
            return DefaultUpdateParam(calculatedModificationPath, cache, param, "text");
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, CommunicationInfo.Param param)
        {
            return DefaultDumpParam(cache, param, "text");
        }
    }
}
