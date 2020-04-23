using System;
using AIProject;
using UnityEx;
using Object = UnityEngine.Object;

namespace IllusionMods
{
    public partial class TextDump
    {
        public static partial class Helpers
        {
            public static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : Object
            {
                try
                {
                    return AssetUtility.LoadAsset<T>(assetBundleInfo);
                }
                catch (Exception err)
                {
                    Logger.LogError(
                        $"ManualLoadAsset<{typeof(T).Name}>({assetBundleInfo.assetbundle} {assetBundleInfo.asset}): {err.Message}\n{err.StackTrace}");
                    return null;
                }
            }
        }
    }
}
