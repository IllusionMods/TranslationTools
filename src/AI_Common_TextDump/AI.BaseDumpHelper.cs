using UnityEngine;
using UnityEx;

namespace IllusionMods
{
    public partial class BaseDumpHelper
    {
        public static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : Object
        {
            return TextDump.Helpers.ManualLoadAsset<T>(assetBundleInfo);
        }
    }
}
