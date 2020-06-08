using System.Collections.Generic;
using System.IO;
using System.Linq;
using ActionGame;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class EventInfoHandler : ParamAssetLoadedHandler<EventInfo, EventInfo.Param>
    {

        public EventInfoHandler(TextResourceRedirector plugin) : base(plugin) { }


        public override IEnumerable<EventInfo.Param> GetParams(EventInfo asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache, EventInfo.Param param)
        {
            var key = TextResourceHelper.GetSpecializedKey(param, param.Name);
            if (string.IsNullOrEmpty(key)) return false;
            var result = false;
            if (cache.TryGetTranslation(key, true, out var translated))
            {
                param.Name = translated;
                TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                result = true;
            }
            else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                     LanguageHelper.IsTranslatable(key))
            {
                cache.AddTranslationToCache(key, !string.IsNullOrEmpty(param.Name) ? param.Name : string.Empty);
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, EventInfo.Param param)
        {
            var key = TextResourceHelper.GetSpecializedKey(param, param.Name);
            var value = !string.IsNullOrEmpty(key) ? key : string.Empty;
            if (string.IsNullOrEmpty(key) || !LanguageHelper.IsTranslatable(key)) return false;
            cache.AddTranslationToCache(key, value);
            return true;
        }
    }
}
