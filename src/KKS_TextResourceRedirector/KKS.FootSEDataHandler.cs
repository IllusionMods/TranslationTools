using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class FootSEDataHandler : ParamAssetLoadedHandler<FootSEData, FootSEData.Param>
    {
        public FootSEDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<FootSEData.Param> GetParams(FootSEData asset)
        {
            return asset.param;
        }

        private static void ApplyTranslation(string calculatedModificationPath, FootSEData.Param param, string value)
        {
            param.supplement = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            FootSEData.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.supplement, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, FootSEData.Param param)
        {
            return DefaultDumpParam(cache, param, param.supplement);
        }
    }
}
