using UnityEx;
namespace IllusionMods
{
    public partial class AssetBundleAddress
    {
        public static explicit operator AssetBundleInfo(AssetBundleAddress aba)
        {
            return new AssetBundleInfo(aba.Name, aba.AssetBundle, aba.Asset, aba.Manifest);
        }

        public static explicit operator AssetBundleAddress(AssetBundleInfo abi)
        {
            return new AssetBundleAddress(abi.name, abi.assetbundle, abi.asset, abi.manifest);
        }
    }
}
