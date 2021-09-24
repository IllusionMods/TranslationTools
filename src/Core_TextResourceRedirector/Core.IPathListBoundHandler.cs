using JetBrains.Annotations;
using UnityEngine;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public static partial class PathListBoundHandlerExtensions
    {
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

