using System;
#if AI 
using AIProject;
using UnityEx;
#endif
#if HS2
using Manager;
#endif


using Object = UnityEngine.Object;

namespace IllusionMods
{
    public partial class TextDump
    {
        internal static partial class Helpers
        {
            internal static T ManualLoadAsset<T>(AssetBundleInfo assetBundleInfo) where T : Object
            {
                return IllusionMods.AssetLoader.ManualLoadAsset<T>(assetBundleInfo);
            }

        }
    }
}
