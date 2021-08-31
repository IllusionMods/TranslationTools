using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public static class HandlerHelper
    {

        private static ResourceMappingPath[] _emptyMappingPathResult = new ResourceMappingPath[0];

        private static IEnumerable<Stream> GetRedirectionStreamsForPath(string path)
        {
            return RedirectedDirectory.GetFilesInDirectory(path, ".txt").Select(x => x.OpenStream());
}

        public static IEnumerable<Stream> GetRedirectionStreams(string calculatedModificationPath, UnityEngine.Object asset, IAssetOrResourceLoadedContext context, bool useMapping=false)
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
                foreach (var entry in RedirectedDirectory.GetFilesInDirectory(mappedPath.CalculatedModificationPath, ".txt"))
                {
                    TextResourceRedirector.Logger.LogDebug(
                        $"{nameof(GetRedirectionStreams)}: {calculatedModificationPath}: adding fallback: {entry.FullName}");
                    if (handled.Contains(entry.FullName)) continue;
                    yield return entry.OpenStream();
                    handled.Add(entry.FullName);
                }
            }
        }

        private static IEnumerable<ResourceMappingPath> GetMappingForPath(string calculatedModificationPath, UnityEngine.Object asset, IAssetOrResourceLoadedContext context)
        {
            return TextResourceRedirector.Instance.TextResourceHelper.ResourceMappingHelper.GetMappingForPath(
                ResourceMappingPath.FromAssetContext(calculatedModificationPath, asset, context),
                ResourceMappingMode.Replacement);
        }
    }
}
