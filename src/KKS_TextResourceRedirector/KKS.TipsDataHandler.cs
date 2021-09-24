using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class TipsDataHandler : ParamAssetLoadedHandler<TipsData, TipsData.Param>
    {
        public TipsDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<TipsData.Param> GetParams(TipsData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TipsData.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, nameof(param.text),
                nameof(param.title));
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TipsData.Param param)
        {
            return DefaultDumpParamMembers(cache, param, nameof(param.text), nameof(param.title));
        }
    }
}
