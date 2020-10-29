using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TitleSkillNameHandler : ParamAssetLoadedHandler<TitleSkillName, TitleSkillName.Param>
    {
        public TitleSkillNameHandler(TextResourceRedirector plugin, bool allowTranslationRegistration = false) :
            base(plugin, allowTranslationRegistration) { }

        public override IEnumerable<TitleSkillName.Param> GetParams(TitleSkillName asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            TitleSkillName.Param param)
        {
            var result = false;
            var key = param.name0;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                param.name0 = translated;
                TrackReplacement(calculatedModificationPath, key, translated);
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                cache.AddTranslationToCache(key, !string.IsNullOrEmpty(param.name1) ? param.name1 : key);
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, TitleSkillName.Param param)
        {
            var key = TextResourceHelper.GetSpecializedKey(param, param.name0);
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var value = !string.IsNullOrEmpty(param.name1) ? param.name1 : key;
            cache.AddTranslationToCache(key, value);
            return true;
        }

        protected override bool ShouldHandleAsset(TitleSkillName asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}])?");
            var result = base.ShouldHandleAsset(asset, context);
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }
    }
}
