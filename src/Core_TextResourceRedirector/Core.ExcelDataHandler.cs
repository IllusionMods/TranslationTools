using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class ExcelDataHandler : AssetLoadedHandlerBaseV2<ExcelData>, IPathListBoundHandler
    {


        /// <summary>
        /// List of column names handler will replace. If empty attempts to replace any translatable column.
        /// </summary>
        public List<string> SupportedColumnNames { get; private set; } = new List<string>();

        private readonly TextResourceHelper _textResourceHelper;

        public ExcelDataHandler(TextResourceHelper helper)
        {
            CheckDirectory = true;
            _textResourceHelper = helper;
            Logger.LogInfo($"{GetType()} enabled");
        }

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        protected override string CalculateModificationFilePath(ExcelData asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, ExcelData asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            var columnsToDump = new HashSet<int>(_textResourceHelper.GetSupportedExcelColumns(calculatedModificationPath, asset));
           
            for (var i = 1; i < asset.list.Count; i++)
            {
                var row = asset.GetRow(i);
                var rowColumns = Enumerable.Range(0, row.Count);
                if (columnsToDump.Count > 0)
                {
                    rowColumns = rowColumns.Where(columnsToDump.Contains);
                }
                
                foreach (var key in rowColumns.Select(j => row[j]).Where(k=>!k.IsNullOrEmpty() && LanguageHelper.IsTranslatable(k)))
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
                new HashSet<int>(_textResourceHelper.GetSupportedExcelColumns(calculatedModificationPath, asset));

            var filter = columnsToTranslate.Count > 0;


            foreach (var param in asset.list)
            {
                for (var i = 0; i < param.list.Count; i++)
                {
                    if (filter && !columnsToTranslate.Contains(i)) continue;
                    var key = param.list[i];
                    if (string.IsNullOrEmpty(key)) continue;
                    if (cache.TryGetTranslation(key, true, out var translated))
                    {
                        result = true;
                        TranslationHelper.RegisterRedirectedResourceTextToPath(translated,
                            calculatedModificationPath);
                        param.list[i] = translated;
                    }
                    else if (LanguageHelper.IsTranslatable(key))
                    {
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
            var result = !context.HasReferenceBeenRedirectedBefore(asset) && this.IsPathAllowed(asset, context);
            Logger.DebugLogDebug($"{GetType()}.ShouldHandleAsset({asset.name}[{asset.GetType()}]) => {result}");
            return result;
        }

        #region IPathListBoundHandler

        public PathList WhiteListPaths { get; } = new PathList();
        public PathList BlackListPaths { get; } = new PathList();

        #endregion IPathListBoundHandler
    }

}
