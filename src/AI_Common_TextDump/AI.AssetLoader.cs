using System;
using System.Collections.Generic;
using System.Text;
using ADV.Commands.Object;
using AIProject;
using UnityEx;

namespace IllusionMods
{
    internal static partial class AssetLoader
    {
        internal static partial class Loader<T>
        {
            internal static T AssetUtilityLoader(AssetBundleInfo assetBundleInfo)
            {
                return AssetUtility.LoadAsset<T>(assetBundleInfo);
            }

            internal static T AssetUtilityLoader(string assetBundle, string assetName, string manifest)
            {
                return AssetUtility.LoadAsset<T>(assetBundle, assetName, manifest);
            }
        }
    }
}
