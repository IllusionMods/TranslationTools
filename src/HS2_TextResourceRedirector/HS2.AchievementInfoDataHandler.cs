using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class AchievementInfoDataHandler : StringArrayParamAssetLoadedHandler<AchievementInfoData, AchievementInfoData.Param>
    {
        public AchievementInfoDataHandler(TextResourceRedirector plugin) : base(plugin) { }

        public override IEnumerable<AchievementInfoData.Param> GetParams(AchievementInfoData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            AchievementInfoData.Param param)
        {
            var result = new List<bool>
            {
                UpdateParamField(calculatedModificationPath, cache, ref param.content),
                UpdateParamField(calculatedModificationPath, cache, ref param.title)
            };
            return result.Any(x => x);
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, AchievementInfoData.Param param)
        {
            var result = new List<bool>
            {
                DumpParamField(cache, param.content),
                DumpParamField(cache, param.title)
            };
            return result.Any(x => x);
        }


    }
}
