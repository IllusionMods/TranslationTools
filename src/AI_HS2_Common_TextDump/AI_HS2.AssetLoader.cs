using System;
using System.Collections.Generic;
using System.Text;
#if AI
using UnityEx;
#endif
#if HS2
using Manager;
#endif

namespace IllusionMods
{
    internal static partial class AssetLoader
    {

        public static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : UnityEngine.Object
        {
            if (!AssetBundleCheck.IsFile(assetBundleInfo.assetbundle))
            {
                Logger.LogWarning($"AssetLoader: No such asset bundle: {assetBundleInfo.assetbundle}");
                return null;
            }
            var loaders = new Loader<T>.AssetBundleInfoLoader[]
            {
#if AI
                Loader<T>.AssetUtilityLoader,
#endif
                Loader<T>.DefaultLoader
            };

            foreach (var forceUnload in new[] {false, true})
            {
                foreach (var loader in loaders)
                {
                    if (TryLoader<T>(() => loader(assetBundleInfo), loader.Method.Name, assetBundleInfo.assetbundle,
                        assetBundleInfo.asset, forceUnload, out var result))
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        internal static partial class Loader<T>
        {
            internal delegate T AssetBundleInfoLoader(AssetBundleInfo assetBundleInfo);
            internal static T DefaultLoader(AssetBundleInfo assetBundleInfo)
            {
                return ManualLoadAsset<T>(assetBundleInfo.assetbundle, assetBundleInfo.asset,
                    assetBundleInfo.manifest);
            }

            internal static T AddressAsInfoLoader(AssetBundleAddress assetBundleAddress)
            {
                return ManualLoadAsset<T>((AssetBundleInfo) assetBundleAddress);
            }


            internal static T AssetBundleManagerLoadAssetLoader(string assetBundle, string assetName, string manifest)
            {
                return AssetBundleManager.LoadAsset(assetBundle, assetName, typeof(T), manifest).GetAsset<T>();
            }
        }
    }
}
