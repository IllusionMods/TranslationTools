using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class WhereLiveDataHandler :ParamAssetLoadedHandler<WhereLiveData, WhereLiveData.Param>
    {
        public WhereLiveDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<WhereLiveData.Param> GetParams(WhereLiveData asset)
        {
            return asset.param;
        }

        private static void ApplyTranslation(string calculatedModificationPath,  WhereLiveData.Param param, string value)
        {
            param.Explan = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            WhereLiveData.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.Explan, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, WhereLiveData.Param param)
        {
            return DefaultDumpParam(cache, param, param.Explan);
        }
    }
}
