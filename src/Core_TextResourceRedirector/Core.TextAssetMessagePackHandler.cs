# if false
using System.IO;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TextAssetMessagePackHandler : TextAssetLoadedHandlerBase
    {
        public TextAssetMessagePackHandler()
        {
            CheckDirectory = true;
            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => TextAssetMessagePackHelper.Enabled;

        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            if (TextAssetMessagePackHelper.CanHandleAsset(asset, context, out var handler))
            {
                //return new TextAndEncoding(asset.bytes, null);
                var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
                var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
                var streams = redirectedResources.Select(x => x.OpenStream());
                var cache = new SimpleTextTranslationCache(
                    defaultTranslationFile,
                    streams,
                    false,
                    true);

                if (cache.IsEmpty)
                {
                    Logger.LogDebug($"{GetType()} unable to handle {calculatedModificationPath} (no cache)");
                    return null;
                }

                var obj = handler.Load(asset);

                if (obj != null && handler.Translate(ref obj, cache, calculatedModificationPath))
                {
                    Logger.LogDebug($"{GetType()} handled {calculatedModificationPath}");
                    return handler.Store(obj);
                }
            }

            return null;
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            return false; //throw new NotImplementedException();
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            var result = Enabled && TextAssetMessagePackHelper.CanHandleAsset(asset, context) &&
                         !context.HasReferenceBeenRedirectedBefore(asset);
            Logger.LogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }
    }
}

#endif
