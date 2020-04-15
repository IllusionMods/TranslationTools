#if !HS

using BepInEx.Logging;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    using BepinLogLevel = BepInEx.Logging.LogLevel;
    public class TextAssetTableHandler : TextAssetLoadedHandlerBase
    {
        private readonly TextAssetTableHelper textAssetTableHelper;
        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => textAssetTableHelper?.Enabled ?? false;

        public TextAssetTableHandler(TextAssetTableHelper textAssetTableHelper)
        {
            CheckDirectory = true;
            this.textAssetTableHelper = textAssetTableHelper;
            Logger.LogInfo($"{this.GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            if (!Enabled || !textAssetTableHelper.IsTable(asset))
            {
                return null;
            }

            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
               outputFile: defaultTranslationFile,
               inputStreams: streams,
               allowTranslationOverride: false,
               closeStreams: true);

            string doTranslation(string cellText)
            {
                if (cache.TryGetTranslation(cellText, false, out string newText))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(newText, calculatedModificationPath);
                    return newText;
                }
                else if (!string.IsNullOrEmpty(cellText) && LanguageHelper.IsTranslatable(cellText))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(cellText, calculatedModificationPath);
                    if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                    {
                        cache.AddTranslationToCache(cellText, cellText);
                    }
                }
                
                return null;
            }

            if (textAssetTableHelper.TryTranslateTextAsset(ref asset, doTranslation, out string result))
            {
                return new TextAndEncoding(result, textAssetTableHelper.TextAssetEncoding);
            }
            return null;
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            throw new NotImplementedException();
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            var result = Enabled && textAssetTableHelper.IsTable(asset) && !context.HasReferenceBeenRedirectedBefore(asset);
            return result;
        }
    }
}

#else //Stub for HS
namespace IllusionMods
{
    public class TextAssetTableHandler
    {
        public TextAssetTableHandler(TextAssetTableHelper textAssetTableHelper) { }
    }
}
#endif
