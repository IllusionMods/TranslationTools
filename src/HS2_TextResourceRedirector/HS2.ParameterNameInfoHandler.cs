using System.Collections.Generic;
using System.Linq;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class
        ParameterNameInfoHandler : StringArrayParamAssetLoadedHandler<ParameterNameInfo, ParameterNameInfo.Param>
    {
        public ParameterNameInfoHandler(TextResourceRedirector plugin) : base(plugin) { }

        public override IEnumerable<ParameterNameInfo.Param> GetParams(ParameterNameInfo param)
        {
            return param.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ParameterNameInfo.Param param)
        {
            var result = new List<bool>
            {
                UpdateParamField(calculatedModificationPath, cache, ref param.trait, "TRAIT:"),
                UpdateParamField(calculatedModificationPath, cache, ref param.mind, "MIND:"),
                UpdateParamField(calculatedModificationPath, cache, ref param.state, "STATE:"),
                UpdateParamField(calculatedModificationPath, cache, ref param.hattribute, "HATTRIBUTE:")
            };
            return result.Any(x => x);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, ParameterNameInfo.Param param)
        {
            var result = new List<bool>
            {
                DumpParamField(cache, param.trait),
                DumpParamField(cache, param.mind),
                DumpParamField(cache, param.state),
                DumpParamField(cache, param.hattribute)
            };
            return result.Any(x => x);
        }
    }
}
