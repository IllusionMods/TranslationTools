﻿#if !HS
using System;
using System.Collections.Generic;
using System.IO;
using IllusionMods.Shared;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;
#if AI || HS2
using AIProject;
#endif

namespace IllusionMods
{
    public class TextAssetTableHandler : RedirectorTextAssetLoadedHandlerBase, IPathListBoundHandler
    {
        public delegate bool TableRulesGetter(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context,
            out HashSet<int> rowWhitelist, out HashSet<int> rowBlacklist,
            out HashSet<int> colWhitelist, out HashSet<int> colBlacklist);


        public TextAssetTableHandler(TextResourceRedirector plugin) : base(plugin, "containing tables") { }


        public TextAssetTableHelper TextAssetTableHelper
        {
            get
            {
                TextAssetTableHelper result = null;
                Plugin.SafeProc(p => p.TextResourceHelper.SafeProc(t => result = t.TableHelper));
                return result;
            }
        }

        // ReSharper disable once CollectionNeverUpdated.Global
        public List<TableRulesGetter> TableRulesGetters { get; } = new List<TableRulesGetter>();


        public override TextAndEncoding TranslateTextAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            var handled = false;
            var start = Time.realtimeSinceStartup;
            try
            {
                Logger.DebugLogDebug("{0}.{1} attempt to handle {2}", GetType(), nameof(ReplaceOrUpdateAsset),
                    calculatedModificationPath);
                if (!Enabled || !TextAssetTableHelper.IsTable(asset))
                {
                    Logger.DebugLogDebug("{0}.{1} unable to handle {2}", GetType(),
                        nameof(ReplaceOrUpdateAsset), calculatedModificationPath);
                    return null;
                }

                var cache = GetTranslationCache(calculatedModificationPath, asset, context);


                if (cache.IsEmpty)
                {
                    Logger.DebugLogDebug("{0}.{1} unable to handle {2} (no cache)", GetType(),
                        nameof(ReplaceOrUpdateAsset), calculatedModificationPath);
                    return null;
                }

                GetTableRules(calculatedModificationPath, asset, context, out var rowAllowed, out var colAllowed);


                bool DoTranslation(int rowIndex, int colIndex, string cellText, out string newCellText)
                {
                    newCellText = null;
                    if (rowAllowed != null && !rowAllowed(rowIndex) || colAllowed != null && !colAllowed(colIndex))
                    {
                        return false;
                    }

                    if (cache.TryGetTranslation(cellText, false, out newCellText))
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

                if (TextAssetTableHelper.TryTranslateTextAsset(ref asset, DoTranslation, out var result))
                {
                    Logger.DebugLogDebug("{0}.{1} handled {2}", GetType(), nameof(ReplaceOrUpdateAsset),
                        calculatedModificationPath);
                    Logger.DebugLogDebug($"{GetType()} handled {calculatedModificationPath}");
                    handled = true;
                    return new TextAndEncoding(result, TextAssetTableHelper.TextAssetEncoding);
                }

                Logger.DebugLogDebug("{0}.{1} unable to handle {2}", GetType(), nameof(ReplaceOrUpdateAsset),
                    calculatedModificationPath);
                return null;
            }
            finally
            {
                Logger.DebugLogDebug("{0}.{1}: {2} => {3} ({4} seconds)", GetType(), nameof(ReplaceOrUpdateAsset),
                    calculatedModificationPath, handled, Time.realtimeSinceStartup - start);
            }
        }


        protected override bool DumpAsset(string calculatedModificationPath, TextAsset asset,
            IAssetOrResourceLoadedContext context)
        {
            // TODO: dump
            return false;
        }
        protected override bool ShouldHandleAsset(TextAsset asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.{nameof(ShouldHandleAsset)}({asset.name}[{asset.GetType()}])?");
            var result = base.ShouldHandleAsset(asset, context) && TextAssetTableHelper.IsTable(asset);
            Logger.DebugLogDebug($"{GetType()}.{nameof(ShouldHandleAsset)}({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }

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
                        return r => whitelist.Contains(r) && !blacklist.Contains(r);
                    }

                    return r => !blacklist.Contains(r) && whitelist.Contains(r);
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
#pragma warning disable IDE0060 // Remove unused parameter
        public TextAssetTableHandler(TextResourceRedirector plugin) { }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
#endif
