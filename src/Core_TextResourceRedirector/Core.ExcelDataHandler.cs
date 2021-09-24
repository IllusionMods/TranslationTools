using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class ExcelDataHandler : RedirectorAssetLoadedHandlerBase<ExcelData>, IPathListBoundHandler
    {
        public ExcelDataHandler(TextResourceRedirector plugin, bool allowTranslationRegistration = false) :
            base(plugin, null, allowTranslationRegistration) { }


        /// <summary>
        ///     List of column names handler will replace. If empty attempts to replace any translatable column.
        /// </summary>
        [UsedImplicitly]
        public List<string> SupportedColumnNames { get; } = new List<string>();

        protected override bool DumpAsset(string calculatedModificationPath, ExcelData asset,
            IAssetOrResourceLoadedContext context)
        {
            var cache = GetDumpCache(calculatedModificationPath, asset, context);

            var columnsToDump =
                new HashSet<int>(TextResourceHelper.GetSupportedExcelColumns(calculatedModificationPath, asset, out var firstRow));

            for (var i = firstRow; i < asset.list.Count; i++)
            {
                var row = asset.GetRow(i);
                var rowColumns = Enumerable.Range(0, row.Count);
                if (columnsToDump.Count > 0)
                {
                    rowColumns = rowColumns.Where(columnsToDump.Contains);
                }

                foreach (var key in rowColumns.Select(j => row[j])
                    .Where(k => !k.IsNullOrEmpty() && LanguageHelper.IsTranslatable(k)))
                {
                    cache.AddTranslationToCache(key, key);
                }
            }

            return true;
        }

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref ExcelData asset,
            IAssetOrResourceLoadedContext context)
        {
            var result = false;
            var start = Time.realtimeSinceStartup;
            try
            {
                Logger.DebugLogDebug("{0}.{1} attempt to handle {2}", GetType(), nameof(ReplaceOrUpdateAsset),
                    calculatedModificationPath);
                var cache = GetTranslationCache(calculatedModificationPath, asset, context);

                if (cache.IsEmpty)
                {
                    Logger.DebugLogDebug("{0}.{1} unable to handle {2} (no cache)", GetType(),
                        nameof(ReplaceOrUpdateAsset), calculatedModificationPath);
                    return false;
                }

                var columnsToTranslate =
                    new HashSet<int>(TextResourceHelper.GetSupportedExcelColumns(calculatedModificationPath, asset, out var firstRow));

                var filter = columnsToTranslate.Count > 0;

                var row = -1;

                var shouldTrack = IsTranslationRegistrationAllowed(calculatedModificationPath);
                foreach (var param in asset.list)
                {
                    row++;
                    if (row < firstRow) continue;
                    if (param.list == null || param.list.Count < 1 || param.list[0] == "no") continue;

                    for (var i = 0; i < param.list.Count; i++)
                    {
                        if (filter && !columnsToTranslate.Contains(i)) continue;

                        foreach (var key in TextResourceHelper.GetExcelRowTranslationKeys(asset.name, param.list, i))
                        {
                            if (string.IsNullOrEmpty(key)) continue;
                            Logger.DebugLogDebug(
                                "Attempting excel replacement [{0}, {1}]: Searching for replacement key={2}", row, i, key);
                            if (cache.TryGetTranslation(key, true, out var translated))
                            {
                                result = true;
                                translated = TextResourceHelper.PrepareTranslationForReplacement(asset, translated);
                                if (shouldTrack) TrackReplacement(calculatedModificationPath, key, translated);
                                TranslationHelper.RegisterRedirectedResourceTextToPath(translated,
                                    calculatedModificationPath);
                                Logger.DebugLogDebug(
                                    "Replacing [{0}, {1}]: key={2}: {3} => {4}", row, i, key,
                                    param.list[i], translated);

                                param.list[i] = translated;
                                break;
                            }

                            if (!LanguageHelper.IsTranslatable(key)) continue;

                            TranslationHelper.RegisterRedirectedResourceTextToPath(key, calculatedModificationPath);
                            if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled)
                            {
                                cache.AddTranslationToCache(key, key);
                            }
                        }
                    }
                }
                return result;
            }
            finally
            {
                Logger.DebugLogDebug("{0}.{1}: {2} => {3} ({4} seconds)", GetType(), nameof(ReplaceOrUpdateAsset), calculatedModificationPath, result, Time.realtimeSinceStartup - start);
            }

        }


        #region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();

        #endregion IPathListBoundHandler
    }
}
