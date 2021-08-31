using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class CommunicationNPCHandler :
        UntestedParamAssetLoadedHandler<CommunicationNPCData, CommunicationNPCData.Param>, IPathListBoundHandler
    {
        public CommunicationNPCHandler(TextResourceRedirector plugin): base(
            plugin, true, false) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<CommunicationNPCData.Param> GetParams(CommunicationNPCData asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            CommunicationNPCData.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, "text");
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, CommunicationNPCData.Param param)
        {
            return DefaultDumpParam(cache, param.text);
        }
    }
}
