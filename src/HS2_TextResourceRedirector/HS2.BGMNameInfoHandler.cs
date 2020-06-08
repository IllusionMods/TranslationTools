using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class BGMNameInfoHandler : StringArrayParamAssetLoadedHandler<BGMNameInfo, BGMNameInfo.Param>
    {
        public BGMNameInfoHandler(TextResourceRedirector plugin) : base(plugin) { }

        public override bool DumpParam(SimpleTextTranslationCache cache, BGMNameInfo.Param param)
        {
            return DumpParamField(cache, param.name);
        }

        public override IEnumerable<BGMNameInfo.Param> GetParams(BGMNameInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            BGMNameInfo.Param param)
        {
            return UpdateParamField(calculatedModificationPath, cache, ref param.name);
        }
    }
}
