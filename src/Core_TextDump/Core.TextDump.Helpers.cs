using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static IllusionMods.TextResourceHelper.Helpers;
using Object = UnityEngine.Object;
#if AI
using AIProject;
using UnityEx;
#endif

namespace IllusionMods
{
    public partial class TextDump
    {
        internal static partial class Helpers
        {
            private static readonly HashSet<string> LoadedBundles = new HashSet<string>();

            public static List<string> GetAllAssetBundleNames()
            {
                return GetAssetBundleNameListFromPath(".", true);
            }


            public static List<string> GetAssetBundleNameListFromPath(string path, bool subdirCheck = false)
            {
                var normPath = NormalizePathSeparators(path);
#if HS2 || KKS
                normPath = normPath.Replace('\\', '/').Trim('/') + "/";
#endif
                return CommonLib.GetAssetBundleNameListFromPath(normPath, subdirCheck);
            }

            public static void UnloadBundles()
            {
                IllusionMods.AssetLoader.UnloadBundles();
#if false
                var bundles = LoadedBundles.ToList();
                LoadedBundles.Clear();
                foreach (var assetBundle in bundles)
                {
                    AssetBundleManager.UnloadAssetBundle(assetBundle, false);
                }
#endif
            }

#if HS
            public static string[] GetAssetNamesFromBundle(string assetBundleName) => AssetBundleCheck.GetAllAssetName(assetBundleName);
#else
            public static string[] GetAssetNamesFromBundle(string assetBundleName)
            {
                string[] ret;

                try
                {
                    ret = AssetBundleCheck.GetAllAssetName(assetBundleName);
                    if (ret?.Length > 0) return ret;
                }
                catch { }


                try
                {
                    ret = AssetBundleCheck.GetAllAssetName(assetBundleName, isAllCheck: true);
                    if (ret?.Length > 0) return ret;
                }
                catch { }



#if !HS2 && !KKS
                AssetBundle assetBundle = null;
                try
                {
                    if (AssetBundleManager.AllLoadedAssetBundleNames.Contains(assetBundleName))
                    {
                        using (var enumerator = AssetBundleManager.ManifestBundlePack.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                var current = enumerator.Current;
                                if (current.Value.LoadedAssetBundles.TryGetValue(assetBundleName,
                                    out var loadedAssetBundle)
                                )
                                {
                                    ret = loadedAssetBundle.m_AssetBundle.GetAllAssetNames().Select(Path.GetFileName)
                                        .ToArray();
                                    if (ret?.Length > 0) return ret;
                                }
                            }
                        }
                    }
                }
                catch { }

                AssetBundleManager.LoadAssetBundleInternal(assetBundleName, false);
                var loadedAssetBundle1 = AssetBundleManager.GetLoadedAssetBundle(assetBundleName, out var err);
                
                if (loadedAssetBundle1 is null)
                {
                    Logger.LogError(err);
                    assetBundle =
                        AssetBundle.LoadFromFile(string.Concat(AssetBundleManager.BaseDownloadingURL, assetBundleName));
                }
                else
                {
                    assetBundle = loadedAssetBundle1.m_AssetBundle;
                }


                try
                {
                    if (assetBundle != null)
                    {
                        ret = assetBundle.GetAllAssetNames().Select(Path.GetFileName).ToArray();
                        if (ret?.Length > 0) return ret;
                    }
                }
                finally
                {
                    if (loadedAssetBundle1 == null && assetBundle != null)
                    {
                        AssetBundleManager.UnloadAssetBundle(assetBundle.name, false);
                    }
                }
#endif


                return new string[0];
            }
#endif

            public static T ManualLoadAsset<T>(AssetBundleAddress assetBundleAddress) where T : Object
            {
                return IllusionMods.AssetLoader.ManualLoadAsset<T>(assetBundleAddress);
#if false
#if AI
                LoadedBundles.Add(assetBundleAddress.AssetBundle);
                return AssetUtility.LoadAsset<T>((AssetBundleInfo) assetBundleAddress);
#else
                return ManualLoadAsset<T>(assetBundleAddress.AssetBundle, assetBundleAddress.Asset,
                    assetBundleAddress.Manifest);
#endif
#endif

            }

            public static Type FindType(string name)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var type = assembly.GetType(name, false);
                        if (type != null)
                        {
                            return type;
                        }
                    }
                    catch
                    {
                        // don't care!
                    }
                }

                return null;
            }



            public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : Object
            {
                return IllusionMods.AssetLoader.ManualLoadAsset<T>(bundle, asset, manifest);
#if false
                LoadedBundles.Add(bundle);
#if AI
                return AssetUtility.LoadAsset<T>(bundle, asset, manifest);
#else
#if HS
                var _ = asset;
                _ = manifest;
                return null;
#else
                manifest = manifest.IsNullOrEmpty() ? null : manifest;
                Logger.DebugLogDebug($"ManualLoadAsset: {bundle}, {asset}, {manifest}");
                try
                {
                    var result = CommonLib.LoadAsset<T>(bundle, asset, false, manifest);
                    AssetBundleManager.UnloadAssetBundle(bundle, true, manifest);
                    return result;
                }
                catch
                {
#if HS2 || KKS
                    throw;
#else
                    AssetBundleManager.LoadAssetBundleInternal(bundle, false, manifest);
                    var assetBundle = AssetBundleManager.GetLoadedAssetBundle(bundle, out var error, manifest);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger?.LogError($"ManualLoadAsset: {error}");
                    }

                    var result = assetBundle.m_AssetBundle.LoadAsset<T>(asset);
                    return result;
#endif
                }
#endif
#endif
#endif
            }
        }
    }
}
