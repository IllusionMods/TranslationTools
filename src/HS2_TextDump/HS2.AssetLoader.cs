using Studio;

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

            internal static T AddObjectAssistLoader(string assetBundle, string assetName, string manifest)
            {
                return AddObjectAssist.LoadAsset<T>(assetBundle, assetName, false, manifest);
            }
        }
    }
}
