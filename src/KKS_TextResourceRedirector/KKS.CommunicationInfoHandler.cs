using ActionGame;
using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class CommunicationInfoHandler : ParamAssetLoadedHandler<CommunicationInfo, CommunicationInfo.Param>,
        IPathListBoundHandler
    {
        public CommunicationInfoHandler(TextResourceRedirector plugin) :
            base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<CommunicationInfo.Param> GetParams(CommunicationInfo asset)
        {
            return DefaultGetParams(asset);
        }

        private static void ApplyTranslation(string calculatedModificationPath, CommunicationInfo.Param param, string value)
        {
            param.text = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            CommunicationInfo.Param param)
        {
            // the multi-quoted string entries need some work, skip them
            if (param.text.Contains("」「")) return false;
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.text, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, CommunicationInfo.Param param)
        {
            return DefaultDumpParam(cache, param, param.text);
        }
    }
}
