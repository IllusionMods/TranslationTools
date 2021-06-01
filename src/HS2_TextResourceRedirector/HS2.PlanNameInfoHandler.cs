using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;

namespace IllusionMods
{
    public class PlanNameInfoHandler : StringArrayParamAssetLoadedHandler<PlanNameInfo, PlanNameInfo.Param>
    {
        private static readonly char[] TrimChars = {'～', '~', ' '};
        public PlanNameInfoHandler(TextResourceRedirector plugin) : base(plugin) { }

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

        protected override void TrackReplacement(string calculatedModificationPath, string orig, string translated,
            HashSet<int> scopes)
        {
            base.TrackReplacement(calculatedModificationPath, orig, translated, scopes);
            var trimmedOrig = orig.Trim(TrimChars);
            if (orig != trimmedOrig)
            {
                base.TrackReplacement(calculatedModificationPath, trimmedOrig, translated.Trim(TrimChars), scopes);
            }
        }
    }
}
