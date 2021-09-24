using Manager;
using UnityObject = UnityEngine.Object;

namespace IllusionMods
{
    public partial class TextDump
    {
        internal static partial class Helpers
        {
            public static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : UnityObject
            {
                return AssetLoader.ManualLoadAsset<T>(assetBundleInfo);
            }
        }
    }
}
