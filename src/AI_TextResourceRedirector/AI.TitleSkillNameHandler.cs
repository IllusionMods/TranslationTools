﻿using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
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
            var origKey = param.name0;
            foreach (var key in TextResourceHelper.GetTranslationKeys(param, origKey))
            {
                if (string.IsNullOrEmpty(key)) continue;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    param.name0 = translated;
                    TrackReplacement(calculatedModificationPath, origKey, translated);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                    result = true;
                    break;
                }

                if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                    LanguageHelper.IsTranslatable(origKey))
                {
                    cache.AddTranslationToCache(key, !string.IsNullOrEmpty(param.name1) ? param.name1 : origKey);
                }
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
    }
}
