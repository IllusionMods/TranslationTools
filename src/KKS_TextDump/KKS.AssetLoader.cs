using Studio;
using static ObiCtrl;

namespace IllusionMods
{
    internal static partial class AssetLoader
    {
        internal static partial class Loader<T>
        {
            internal static T AssetBundleDataLoader(string assetBundle, string assetName, string manifest)
            {
                return new AssetBundleData(assetBundle, assetName).GetAsset<T>();
            }

            internal static T AssetBundleManagerLoadAssetLoader(string assetBundle, string assetName, string manifest)
            {
                return AssetBundleManager.LoadAsset(assetBundle, assetName, typeof(T), manifest).GetAsset<T>();
            }

            
            internal static T AddressAsInfoLoader(AssetBundleAddress assetBundleAddress)
            {
                return ManualLoadAsset<T>(assetBundleAddress.AssetBundle, assetBundleAddress.Name,
                    assetBundleAddress.Manifest);
            }
        }
    }
}
