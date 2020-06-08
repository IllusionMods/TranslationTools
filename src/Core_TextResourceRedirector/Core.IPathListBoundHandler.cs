using UnityEngine;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public interface IPathListBoundHandler
    {
        PathList WhiteListPaths { get; }
        PathList BlackListPaths { get; }
    }

    public static class PathListBoundHandlerExtensions
    {
        public static bool IsPathWhitelisted(this IPathListBoundHandler handler, string path,
            bool isPathNormalized = false)
        {
            return handler.WhiteListPaths.Count == 0 || handler.WhiteListPaths.IsPathListed(path, isPathNormalized);
        }

        public static bool IsPathBlacklisted(this IPathListBoundHandler handler, string path,
            bool isPathNormalized = false)
        {
            return handler.BlackListPaths.Count != 0 && handler.BlackListPaths.IsPathListed(path, isPathNormalized);
        }

        public static bool IsPathAllowed(this IPathListBoundHandler handler, string path, bool isPathNormalized = false)
        {
            var search = isPathNormalized ? path : PathList.Normalize(path);
            TextResourceRedirector.Logger.DebugLogDebug($"IsPathAllowed: {search}");
            if (handler.WhiteListPaths.Count > handler.BlackListPaths.Count)
            {
                return IsPathWhitelisted(handler, search, true) && !IsPathBlacklisted(handler, search, true);
            }

            return !IsPathBlacklisted(handler, search, true) && IsPathWhitelisted(handler, search, true);
        }

        public static bool IsPathBlocked(this IPathListBoundHandler handler, string path, bool isPathNormalized = false)
        {
            return !IsPathAllowed(handler, path, isPathNormalized);
        }


        public static bool IsPathAllowed(this IPathListBoundHandler handler, Object asset,
            IAssetOrResourceLoadedContext context)
        {
            var pth = PathList.Normalize(context.GetUniqueFileSystemAssetPath(asset).Replace(".unity3d", string.Empty));
            return IsPathAllowed(handler, pth, true);
        }

        public static bool IsPathBlocked(this IPathListBoundHandler handler, Object asset,
            IAssetOrResourceLoadedContext context)
        {
            return !IsPathAllowed(handler, asset, context);
        }
    }
}
