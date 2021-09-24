using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class TopicHandler : ParamAssetLoadedHandler<Topic, Topic.Param>
    {
        public TopicHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<Topic.Param> GetParams(Topic asset)
        {
            return asset.param;
        }

        private static void ApplyTranslation(string calculatedModificationPath, Topic.Param param, string value)
        {
            param.Name = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            Topic.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.Name, ApplyTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, Topic.Param param)
        {
            return DefaultDumpParam(cache, param, param.Name);
        }
    }
}
