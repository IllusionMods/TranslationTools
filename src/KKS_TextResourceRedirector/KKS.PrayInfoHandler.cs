using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class PrayInfoHandler : ParamAssetLoadedHandler<PrayInfo,PrayInfo.Param>
    {
        public PrayInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }
        public override IEnumerable<PrayInfo.Param> GetParams(PrayInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache, PrayInfo.Param param)
        {
            var nameResult = UpdateName(calculatedModificationPath, cache, param);
            var explanResult = UpdateExplan(calculatedModificationPath, cache, param);
            return nameResult || explanResult;
        }

        private void ApplyExplanTranslation(string calculatedModificationPath, PrayInfo.Param param, string value)
        {
            param.Explan = value;
        }

        private bool UpdateExplan(string calculatedModificationPath, SimpleTextTranslationCache cache,
            PrayInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.Explan, ApplyExplanTranslation);
        }


        private void ApplyNameTranslation(string calculatedModificationPath, PrayInfo.Param param, string value)
        {
            param.Name = value;
        }

        private bool UpdateName(string calculatedModificationPath, SimpleTextTranslationCache cache, PrayInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.Name, ApplyNameTranslation);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, PrayInfo.Param param)
        {
            var nameResult = DefaultDumpParam(cache, param, param.Name);
            var explanResult = DefaultDumpParam(cache, param, param.Explan);
            return nameResult || explanResult;
        }
    }
}
