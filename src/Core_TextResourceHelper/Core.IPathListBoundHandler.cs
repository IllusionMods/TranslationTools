using BepInEx.Logging;
using JetBrains.Annotations;

namespace IllusionMods
{
    public interface IPathListBoundHandler
    {
        PathList WhiteListPaths { get; }
        PathList BlackListPaths { get; }
    }

    [PublicAPI]
    // ReSharper disable once PartialTypeWithSinglePart
    public static partial class PathListBoundHandlerExtensions
    {
        private static ManualLogSource Logger => TextResourceHelper.Logger;

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
            var result = true;
            try
            {
                // run shorter test first
                if (handler.WhiteListPaths.Count < handler.BlackListPaths.Count)
                {
                    return result = IsPathWhitelisted(handler, search, true) &&
                                    !IsPathBlacklisted(handler, search, true);
                }

                return result = !IsPathBlacklisted(handler, search, true) &&
                                IsPathWhitelisted(handler, search, true);
            }
            finally
            {
                Logger.DebugLogDebug("{0}.{1}: {2} => {3}", handler.GetType(), nameof(IsPathAllowed), search, result);
            }
        }

        public static bool IsPathBlocked(this IPathListBoundHandler handler, string path, bool isPathNormalized = false)
        {
            return !IsPathAllowed(handler, path, isPathNormalized);
        }
    }
}
