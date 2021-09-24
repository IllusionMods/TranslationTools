#if HS2
using Manager;
#endif
using UnityEx;
using UnityObject = UnityEngine.Object;


namespace IllusionMods
{
    public partial class BaseDumpHelper
    {
        public static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : UnityObject
        {
            return AssetLoader.ManualLoadAsset<T>(assetBundleInfo);
        }
    }
}
