# if !HS
using BepInEx.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TextAssetMessagePackHandler : TextAssetLoadedHandlerBase
    {
        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => TextAssetMessagePackHelper.Enabled;

        private static readonly HashSet<string> CanHandle = new HashSet<string>();
        private static readonly HashSet<string> CanNotHandle = new HashSet<string>();
        public TextAssetMessagePackHandler()
        {
            CheckDirectory = true;
            Logger.LogInfo($"{this.GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            if (TextAssetMessagePackHelper.CanHandleAsset(asset, out var handler))
            {
                //return new TextAndEncoding(asset.bytes, null);
                var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
                var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
                var streams = redirectedResources.Select(x => x.OpenStream());
                var cache = new SimpleTextTranslationCache(
                   outputFile: defaultTranslationFile,
                   inputStreams: streams,
                   allowTranslationOverride: false,
                   closeStreams: true);

                if (cache.IsEmpty) return null;

                var obj = handler.Load(asset);
                
                if (obj != null && handler.Translate(ref obj, cache, calculatedModificationPath))
                {
                    return handler.Store(obj);
                }
            }
            return null;
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return false; //throw new NotImplementedException();
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            var result = Enabled && TextAssetMessagePackHelper.CanHandleAsset(asset) && !context.HasReferenceBeenRedirectedBefore(asset);
            return result;
        }
    }
}

#endif
