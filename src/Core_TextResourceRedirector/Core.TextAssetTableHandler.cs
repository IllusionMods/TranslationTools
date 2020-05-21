#if !HS

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using IllusionMods.Shared;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TextAssetTableHandler : TextAssetLoadedHandlerBase, IPathListBoundHandler
    {
        public delegate bool TableRulesGetter(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context,
            out HashSet<int> rowWhitelist, out HashSet<int> rowBlacklist,
            out HashSet<int> colWhitelist, out HashSet<int> colBlacklist);

        private readonly TextAssetTableHelper _textAssetTableHelper;

        public TextAssetTableHandler(TextResourceHelper textResourceHelper)
        {
            CheckDirectory = true;
            _textAssetTableHelper = textResourceHelper.TableHelper;
            Logger.LogInfo($"{GetType()} {(Enabled ? "enabled" : "disabled")}");
        }

        public List<TableRulesGetter> TableRulesGetters { get; } = new List<TableRulesGetter>();
        protected static ManualLogSource Logger => TextResourceRedirector.Logger;

        public bool Enabled => _textAssetTableHelper?.Enabled ?? false;

        private void GetTableRules(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context, out Predicate<int> rowAllowed, out Predicate<int> colAllowed)
        {
            Predicate<int> BuildAllowedPredicate(HashSet<int> whitelist, HashSet<int> blacklist)
            {
                var haveWhitelist = whitelist != null && whitelist.Count > 0;
                var haveBlacklist = blacklist != null && blacklist.Count > 0;


                if (haveWhitelist)
                {
                    if (!haveBlacklist) return whitelist.Contains;

                    if (blacklist.Count > whitelist.Count)
                    {
                        return (r) => whitelist.Contains(r) && !blacklist.Contains(r);
                    }

                    return (r) => !blacklist.Contains(r) && whitelist.Contains(r);

                }

                if (haveBlacklist)
                {
                    return ((Predicate<int>) blacklist.Contains).Not;
                }

                return null;
            }


            foreach (var getter in TableRulesGetters)
            {
                var handled = getter(calculatedModificationPath, asset, context,
                    out var rowWhitelist, out var rowBlacklist,
                    out var colWhitelist, out var colBlacklist);
                if (!handled) continue;

                rowAllowed = BuildAllowedPredicate(rowWhitelist, rowBlacklist);
                colAllowed = BuildAllowedPredicate(colWhitelist, colBlacklist);
                return;
            }

            rowAllowed = null;
            colAllowed = null;

        }
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

            if (cache.IsEmpty)
            {
                Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath} (no cache)");
                return null;
            }

            GetTableRules(calculatedModificationPath, asset, context, out var rowAllowed, out var colAllowed);


            bool DoTranslation(int rowIndex, int colIndex, string cellText, out string newCellText)
            {
                newCellText = null;
                if ((rowAllowed != null && !rowAllowed(rowIndex)) || (colAllowed != null && !colAllowed(colIndex))) return false;

                if (cache.TryGetTranslation(cellText, false, out  newCellText))
                {
                    TranslationHelper.RegisterRedirectedResourceTextToPath(newCellText, calculatedModificationPath);
                    return true;
                }


                if (string.IsNullOrEmpty(cellText) || !LanguageHelper.IsTranslatable(cellText)) return false;

                TranslationHelper.RegisterRedirectedResourceTextToPath(cellText, calculatedModificationPath);
                if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                {
                    cache.AddTranslationToCache(cellText, cellText);
                }

                return false;
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
            var result = Enabled && !context.HasReferenceBeenRedirectedBefore(asset) &&
                         _textAssetTableHelper.IsTable(asset) &&
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
        public TextAssetTableHandler(TextResourceHelper textResourceHelper) { }
    }
}
#endif
