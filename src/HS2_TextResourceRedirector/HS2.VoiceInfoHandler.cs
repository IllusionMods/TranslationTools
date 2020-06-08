using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class VoiceInfoHandler : ParamAssetLoadedHandler<VoiceInfo, VoiceInfo.Param>
    {

        public VoiceInfoHandler(TextResourceRedirector plugin) : base(plugin) { }


        public override IEnumerable<VoiceInfo.Param> GetParams(VoiceInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache, VoiceInfo.Param param)
        {
            var result = false;
            var key = param.Personality;
            if (string.IsNullOrEmpty(key)) return false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                param.Personality = translated;
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                cache.AddTranslationToCache(key, !string.IsNullOrEmpty(param.EnUS) ? param.EnUS : key);
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, VoiceInfo.Param param)
        {
            var key = param.Personality;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            var val = string.IsNullOrEmpty(param.EnUS) ? string.Empty : param.EnUS;
            cache.AddTranslationToCache(key, val);
            return true;
        }
    }
}
