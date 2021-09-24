using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class ResultTopicDataHandler : ParamAssetLoadedHandler<ResultTopicData, ResultTopicData.Param>
    {
        public ResultTopicDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<ResultTopicData.Param> GetParams(ResultTopicData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            ResultTopicData.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.name, ApplyTranslation);
        }


        public override bool DumpParam(SimpleTextTranslationCache cache, ResultTopicData.Param param)
        {
            return DefaultDumpParam(cache, param, param.name);
        }

        private static void ApplyTranslation(string calculatedModificationPath, ResultTopicData.Param param,
            string value)
        {
            param.name = value;
        }
    }
}
