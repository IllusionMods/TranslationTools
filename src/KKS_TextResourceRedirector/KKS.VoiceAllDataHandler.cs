using System.Collections.Generic;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class VoiceAllDataHandler : UntestedParamAssetLoadedHandler<VoiceAllData, VoiceAllData.Param>,
        IPathListBoundHandler
    {
        public VoiceAllDataHandler(TextResourceRedirector plugin) : base(plugin, true, false) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<VoiceAllData.Param> GetParams(VoiceAllData asset)
        {
            return asset.param;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            VoiceAllData.Param param)
        {
            var result = false;

            foreach (var voiceData in param.data)
            {
                foreach (var voiceInfo in voiceData.info)
                {
                    var key = voiceInfo.word;

                    if (string.IsNullOrEmpty(key)) return false;
                    if (cache.TryGetTranslation(key, true, out var translated))
                    {
                        if (!EnableSafeMode.Value)
                        {
                            WarnIfUnsafe(calculatedModificationPath);
                            voiceInfo.word = translated;
                        }

                        TrackReplacement(calculatedModificationPath, key, translated);
                        TranslationHelper.RegisterRedirectedResourceTextToPath(translated,
                            calculatedModificationPath);
                        result = true;
                    }
                    else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                             LanguageHelper.IsTranslatable(key))
                    {
                        cache.AddTranslationToCache(key, string.Empty);
                    }
                }
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, VoiceAllData.Param param)
        {
            var result = false;

            foreach (var voiceData in param.data)
            {
                foreach (var voiceInfo in voiceData.info)
                {
                    if (DefaultDumpParam(cache, voiceInfo.word)) result = true;
                }
            }

            return result;
        }
    }
}
