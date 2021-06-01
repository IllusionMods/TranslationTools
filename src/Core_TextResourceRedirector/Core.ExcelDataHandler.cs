using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
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
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            var columnsToDump =
                new HashSet<int>(TextResourceHelper.GetSupportedExcelColumns(calculatedModificationPath, asset));

            for (var i = 1; i < asset.list.Count; i++)
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
            Logger.DebugLogDebug($"{GetType()} attempt to handle {calculatedModificationPath}");
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            var result = false;

            if (cache.IsEmpty)
            {
                Logger.DebugLogDebug($"{GetType()} unable to handle {calculatedModificationPath} (no cache)");
                return false;
            }

            var columnsToTranslate =
                new HashSet<int>(TextResourceHelper.GetSupportedExcelColumns(calculatedModificationPath, asset));

            var filter = columnsToTranslate.Count > 0;

            var row = -1;

            var shouldTrack = IsTranslationRegistrationAllowed(calculatedModificationPath);
            foreach (var param in asset.list)
            {
                row++;
                if (param.list == null || param.list.Count < 1 || param.list[0] == "no") continue;

                for (var i = 0; i < param.list.Count; i++)
                {
                    if (filter && !columnsToTranslate.Contains(i)) continue;

                    foreach (var key in TextResourceHelper.GetExcelRowTranslationKeys(asset.name, param.list, i))
                    {
                        if (string.IsNullOrEmpty(key)) continue;
                        Logger.DebugLogDebug(
                            $"Attempting excel replacement [{row}, {i}]: Searching for replacement key={key}");
                        if (cache.TryGetTranslation(key, true, out var translated))
                        {
                            result = true;
                            translated = TextResourceHelper.PrepareTranslationForReplacement(asset, translated);
                            if (shouldTrack) TrackReplacement(calculatedModificationPath, key, translated);
                            TranslationHelper.RegisterRedirectedResourceTextToPath(translated,
                                calculatedModificationPath);
                            Logger.DebugLogDebug(
                                $"Replacing [{row}, {i}]: key={key}: {param.list[i]} => {translated}");

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

            Logger.DebugLogDebug(result
                ? $"{GetType()} handled {calculatedModificationPath}"
                : $"{GetType()} unable to handle {calculatedModificationPath}");
            return result;
        }

        protected override bool ShouldHandleAsset(ExcelData asset, IAssetOrResourceLoadedContext context)
        {
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}])?");
            var result = base.ShouldHandleAsset(asset, context);
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }

        #region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();

        #endregion IPathListBoundHandler
    }
}
