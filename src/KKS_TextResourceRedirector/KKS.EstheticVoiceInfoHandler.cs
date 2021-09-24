using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;

namespace IllusionMods
{
    public class EstheticVoiceInfoHandler : ParamAssetLoadedHandler<EstheticVoiceInfo, EstheticVoiceInfo.Param>, IPathListBoundHandler
    {
        // lots of repeating entries, this should save some overhead
        private readonly string[] _lastMatch = {null, null, null};

        public EstheticVoiceInfoHandler(TextResourceRedirector plugin) : base(plugin, true) { }
        public override IEnumerable<EstheticVoiceInfo.Param> GetParams(EstheticVoiceInfo asset)
        {
            return asset.param;
        }

        private void SaveLastMatch(string calculatedModificationPath, string origValue, string transValue)
        {
            _lastMatch[0] = calculatedModificationPath;
            _lastMatch[1] = origValue;
            _lastMatch[2] = transValue;
        }

        private bool ApplyLastMatch(string calculatedModificationPath, EstheticVoiceInfo.VoiceAsset info)
        {
            if (info.voice != _lastMatch[1] || calculatedModificationPath != _lastMatch[0] ) return false;
            info.voice = _lastMatch[2];
            return true;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache, EstheticVoiceInfo.Param param)
        {
            var result = false;
            foreach (var voiceAsset in param.voiceAssets)
            {
                if (string.IsNullOrEmpty(voiceAsset.voice)) continue;
                if (ApplyLastMatch(calculatedModificationPath, voiceAsset))
                {
                    result = true;
                    continue;
                }

                if (cache.TryGetTranslation(voiceAsset.voice, true, out var translated))
                {
                    SaveLastMatch(calculatedModificationPath, voiceAsset.voice, translated);
                    voiceAsset.voice = translated;

                    // Esthetic Scene
                    TrackReplacement(calculatedModificationPath, voiceAsset.voice, translated, 13, -1);
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated,
                        calculatedModificationPath);
                    result = true;
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         LanguageHelper.IsTranslatable(voiceAsset.voice))
                {
                    DefaultDumpParam(cache, param, voiceAsset, voiceAsset.voice);
                }
                
            }

            return result;
        }

        public override bool DumpParam(SimpleTextTranslationCache cache, EstheticVoiceInfo.Param param)
        {
            var result = false;
            foreach (var voiceAsset in param.voiceAssets)
            {
                if (string.IsNullOrEmpty(voiceAsset.voice)) continue;
                if (DefaultDumpParam(cache, param, voiceAsset, voiceAsset.voice)) result = true;
            }

            return result;
        }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();
    }
}
