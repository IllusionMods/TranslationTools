using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class
        EventContentInfoDataHandler : StringArrayParamAssetLoadedHandler<EventContentInfoData,
            EventContentInfoData.Param>
    {
        public EventContentInfoDataHandler(TextResourceRedirector plugin) : base(plugin) { }

        public override bool DumpParam(SimpleTextTranslationCache cache, EventContentInfoData.Param param)
        {
            return DumpParamField(cache, param.eventNames);
        }

        public override IEnumerable<EventContentInfoData.Param> GetParams(EventContentInfoData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            EventContentInfoData.Param param)
        {
            return UpdateParamField(calculatedModificationPath, cache, ref param.eventNames);
        }
    }
}
