#if !HS

using System;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TextAssetTableHandler : TextAssetLoadedHandlerBase, IPathListBoundHandler
    {
        private readonly TextAssetTableHelper _textAssetTableHelper;

        public TextAssetTableHandler(TextAssetTableHelper textAssetTableHelper)
        {
            CheckDirectory = true;
            this._textAssetTableHelper = textAssetTableHelper;
            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => _textAssetTableHelper?.Enabled ?? false;

        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()} attempt to handle {calculatedModificationPath}");
            if (!Enabled || !_textAssetTableHelper.IsTable(asset))
            {
                Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath}");
                return null;
            }

            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            string DoTranslation(string cellText)
            {
                if (cache.TryGetTranslation(cellText, false, out var newText))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(newText, calculatedModificationPath);
                    return newText;
                }

                if (!string.IsNullOrEmpty(cellText) && LanguageHelper.IsTranslatable(cellText))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(cellText, calculatedModificationPath);
                    if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                    {
                        cache.AddTranslationToCache(cellText, cellText);
                    }
                }

                return null;
            }

            if (_textAssetTableHelper.TryTranslateTextAsset(ref asset, DoTranslation, out var result))
            {
                Logger.DebugLogDebug($"{GetType()} handled {calculatedModificationPath}");
                return new TextAndEncoding(result, _textAssetTableHelper.TextAssetEncoding);
            }
            Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath}");
            return null;
        }

        protected override string CalculateModificationFilePath(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            throw new NotImplementedException();
        }

        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}])?");
            var result = Enabled && !context.HasReferenceBeenRedirectedBefore(asset) && _textAssetTableHelper.IsTable(asset) &&
                   this.IsPathAllowed(asset, context);
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }

        #region IPathListBoundHandler
        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();
        #endregion IPathListBoundHandler
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
