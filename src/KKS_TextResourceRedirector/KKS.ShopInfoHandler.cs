using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class ShopInfoHandler : UntestedParamAssetLoadedHandler<ShopInfo, ShopInfo.Param>
    {
        public ShopInfoHandler(TextResourceRedirector plugin) : base(plugin, true, false) { }

        public override IEnumerable<ShopInfo.Param> GetParams(ShopInfo asset)
        {
            return DefaultGetParams(asset);
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ShopInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, nameof(param.Name), nameof(param.Explan),
                nameof(param.NumText));
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, ShopInfo.Param param)
        {
            return DefaultDumpParam(cache, param, nameof(param.Name), nameof(param.Explan), nameof(param.NumText));
        }
    }
}
