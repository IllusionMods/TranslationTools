using AIProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IllusionMods
{
    public partial class BaseDumpHelper
    {
        public static T ManualLoadAsset<T>(UnityEx.AssetBundleInfo assetBundleInfo) where T : UnityEngine.Object
        {
            try
            {
                return AssetUtility.LoadAsset<T>(assetBundleInfo);
            }
            catch (Exception err)
            {
                TextDump.Logger.LogError($"ManualLoadAsset<{typeof(T).Name}>({assetBundleInfo.assetbundle} {assetBundleInfo.asset}): {err.Message}\n{err.StackTrace}");
                return null;
            }
        }
    }
}
