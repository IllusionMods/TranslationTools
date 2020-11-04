using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class PlanNameInfoHandler : StringArrayParamAssetLoadedHandler<PlanNameInfo, PlanNameInfo.Param>
    {
        public PlanNameInfoHandler(TextResourceRedirector plugin) : base(plugin) { }

        private static readonly char[] TrimChars = new[] {'～', '~', ' '};

        public override bool DumpParam(SimpleTextTranslationCache cache, PlanNameInfo.Param param)
        {
            return DumpParamField(cache, param.name);
        }

        public override IEnumerable<PlanNameInfo.Param> GetParams(PlanNameInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            PlanNameInfo.Param param)
        {
            return UpdateParamField(calculatedModificationPath, cache, ref param.name);
        }

        protected override void TrackReplacement(string calculatedModificationPath, string orig, string translated)
        {
            base.TrackReplacement(calculatedModificationPath, orig, translated);
            var trimmedOrig = orig.Trim(TrimChars);
            if (orig != trimmedOrig)
            {
                base.TrackReplacement(calculatedModificationPath, trimmedOrig, translated.Trim(TrimChars));
            }
        }
    }
}
