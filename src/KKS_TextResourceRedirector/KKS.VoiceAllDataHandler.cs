using System;
using System.Collections.Generic;
using ADV;
using HarmonyLib;
using UnityEngine.SceneManagement;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;
using static SaveData.FileQuestion;

namespace IllusionMods
{
    public class VoiceAllDataHandler : ParamAssetLoadedHandler<VoiceAllData, VoiceAllData.Param>,
        IPathListBoundHandler
    {

        public VoiceAllDataHandler(TextResourceRedirector plugin) : base(plugin, true) { }

        public PathList WhiteListPaths { get; } = new PathList();

        public PathList BlackListPaths { get; } = new PathList();

        public override IEnumerable<VoiceAllData.Param> GetParams(VoiceAllData asset)
        {
            return asset.param;
        }

        // lots of repeating entries, this should save some overhead
        private readonly string[] _lastMatch = {null, null, null};

        private void SaveLastMatch(string calculatedModificationPath, string origValue, string transValue)
        {
            _lastMatch[0] = calculatedModificationPath;
            _lastMatch[1] = origValue;
            _lastMatch[2] = transValue;
        }

        private bool ApplyLastMatch(string calculatedModificationPath, VoiceAllData.VoiceInfo info)
        {
            if (info.word != _lastMatch[1] || calculatedModificationPath != _lastMatch[0] ) return false;
            info.word = _lastMatch[2];
            return true;
        }

        public override bool UpdateParam(string calculatedModificationPath, SimpleTextTranslationCache cache,
            VoiceAllData.Param param)
        {
            var result = false;

            foreach (var voiceData in param.data)
            {
                foreach (var voiceInfo in voiceData.info)
                {
                    if (string.IsNullOrEmpty(voiceInfo.word)) continue;
                    if (ApplyLastMatch(calculatedModificationPath, voiceInfo))
                    {
                        result = true;
                        continue;
                    }

                    if (cache.TryGetTranslation(voiceInfo.word, true, out var translated))
                    {
                        SaveLastMatch(calculatedModificationPath, voiceInfo.word, translated);
                        voiceInfo.word = translated;

                        // H & Free-H scopes
                        TrackReplacement(calculatedModificationPath, voiceInfo.word, translated, 8, 9, -1);
                        TranslationHelper.RegisterRedirectedResourceTextToPath(translated,
                            calculatedModificationPath);
                        result = true;
                    }
                    else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                             LanguageHelper.IsTranslatable(voiceInfo.word))
                    {
                        DefaultDumpParam(cache, param, voiceInfo, voiceInfo.word);
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
                    if (DefaultDumpParam(cache, param, voiceInfo, voiceInfo.word)) result = true;
                }
            }

            return result;
        }

        public override bool ShouldHandleAssetForContext(VoiceAllData asset, IAssetOrResourceLoadedContext context)
        {
            // loaded multiple times in same context, needs to always replace
            return true;
        }
    }
}
