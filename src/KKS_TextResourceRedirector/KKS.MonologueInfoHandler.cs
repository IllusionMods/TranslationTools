using System.Collections.Generic;
using ActionGame;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class MonologueInfoHandler : ParamAssetLoadedHandler<MonologueInfo, MonologueInfo.Param>
    {
        public MonologueInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public override IEnumerable<MonologueInfo.Param> GetParams(MonologueInfo asset)
        {
            return asset.param;
        }

        private void ApplyTranslation(string calculatedModificationPath, MonologueInfo.Param param, string value)
        {
            param.Text = value;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            MonologueInfo.Param param)
        {
            return DefaultUpdateParam(calculatedModificationPath, cache, param, param.Text, ApplyTranslation);
        }



        public override bool DumpParam(SimpleTextTranslationCache cache, MonologueInfo.Param param)
        {
            return DefaultDumpParam(cache, param, param.Text);
        }
    }
}
