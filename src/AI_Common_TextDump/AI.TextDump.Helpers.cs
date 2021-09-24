#if AI 
using UnityEx;
#endif
#if HS2
using Manager;
#endif
using UnityObject = UnityEngine.Object;

namespace IllusionMods
{
    public partial class TextDump
    {
        internal static partial class Helpers
        {
            internal static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : UnityObject
            {
                return IllusionMods.AssetLoader.ManualLoadAsset<T>(assetBundleInfo);
            }

        }
    }
}
