using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;
using Object = UnityEngine.Object;

namespace IllusionMods
{
    public static class HandlerHelper
    {
        internal const string DefaultTranslationFileName = "translation.txt";

        private static ResourceMappingPath[] _emptyMappingPathResult = new ResourceMappingPath[0];

        public static IEnumerable<Stream> GetRedirectionStreams(string calculatedModificationPath, Object asset,
            IAssetOrResourceLoadedContext context, bool useMapping = false)
        {
            var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt"))
            {
                if (handled.Contains(entry.FullName)) continue;
                yield return entry.OpenStream();
                handled.Add(entry.FullName);
            }

            if (!useMapping) yield break;

            foreach (var mappedPath in GetMappingForPath(calculatedModificationPath, asset, context))
            {
                foreach (var entry in RedirectedDirectory.GetFilesInDirectory(mappedPath.CalculatedModificationPath,
                    ".txt"))
                {
                    TextResourceRedirector.Logger.LogDebug(
                        $"{nameof(GetRedirectionStreams)}: {calculatedModificationPath}: adding fallback: {entry.FullName}");
                    if (handled.Contains(entry.FullName)) continue;
                    yield return entry.OpenStream();
                    handled.Add(entry.FullName);
                }
            }
        }

        public static SimpleTextTranslationCache GetDumpCache<TAsset>(this IRedirectorHandler handler,
            string calculatedModificationPath, TAsset asset,
            IAssetOrResourceLoadedContext context) where TAsset : Object
        {
            return handler.GetTranslationCache(calculatedModificationPath, asset, context, false);
        }


        public static SimpleTextTranslationCache GetTranslationCache<TAsset>(this IRedirectorHandler handler,
            string calculatedModificationPath, TAsset asset,
            IAssetOrResourceLoadedContext context, bool includeStreams = true) where TAsset : Object
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, DefaultTranslationFileName);
            var streams =
                GetRedirectionStreams(calculatedModificationPath, asset, context, handler.EnableFallbackMapping);
            if (!includeStreams)
            {
                return new SimpleTextTranslationCache(
                    defaultTranslationFile,
                    false);
            }

            return new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);
        }

        private static IEnumerable<Stream> GetRedirectionStreamsForPath(string path)
        {
            return RedirectedDirectory.GetFilesInDirectory(path, ".txt").Select(x => x.OpenStream());
        }

        private static IEnumerable<ResourceMappingPath> GetMappingForPath(string calculatedModificationPath,
            Object asset, IAssetOrResourceLoadedContext context)
        {
            return TextResourceRedirector.Instance.TextResourceHelper.ResourceMappingHelper.GetMappingForPath(
                ResourceMappingPath.FromAssetContext(calculatedModificationPath, asset, context),
                ResourceMappingMode.Replacement);
        }
    }
}
