using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class CommunicationNPCHandler :
        ParamAssetLoadedHandler<CommunicationNPCData, CommunicationNPCData.Param>, IPathListBoundHandler
    {
        public CommunicationNPCHandler(TextResourceRedirector plugin): base(
            plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<CommunicationNPCData.Param> GetParams(CommunicationNPCData asset)
        {
            return DefaultGetParams(asset);
        }

        private static void ApplyTranslation(string calculatedModificationPath, CommunicationNPCData.Param param, string value)
        {
            param.text = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            CommunicationNPCData.Param param)
        {
            // the multi-quoted string entries need some work, skip them
            if (param.text.Contains("」「")) return false;
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.text, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, CommunicationNPCData.Param param)
        {
            return DefaultDumpParam(cache, param, param.text);
        }
    }
}
