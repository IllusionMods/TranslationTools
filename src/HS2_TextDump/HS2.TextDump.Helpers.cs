using Manager;
using UnityEngine;

namespace IllusionMods
{
    public partial class TextDump
    {
        internal static partial class Helpers
        {
            public static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : Object
            {
                return AssetLoader.ManualLoadAsset<T>(assetBundleInfo);
            }
        }
    }
}
