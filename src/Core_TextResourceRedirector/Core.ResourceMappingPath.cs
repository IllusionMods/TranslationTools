using XUnity.ResourceRedirector;
using UnityEngineObject = UnityEngine.Object;

namespace IllusionMods
{
    public partial class ResourceMappingPath
    {
        public static ResourceMappingPath FromAssetContext(string calculatedModificationPath, UnityEngineObject asset,
            IAssetOrResourceLoadedContext context)
        {
            return new ResourceMappingPath(
                context.GetUniqueFileSystemAssetPath(asset).Replace(".unity3d", string.Empty),
                calculatedModificationPath, allPathsNormalized: true);
        }
    }
}
