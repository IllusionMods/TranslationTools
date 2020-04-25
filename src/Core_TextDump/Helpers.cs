using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
#if AI
using AIProject;

#endif

namespace IllusionMods
{
    public partial class TextDump
    {
        public static partial class Helpers
        {
            private static readonly HashSet<string> LoadedBundles = new HashSet<string>();

            public static List<string> GetAllAssetBundleNames()
            {
                return GetAssetBundleNameListFromPath(".", true);
            }


            public static List<string> GetAssetBundleNameListFromPath(string path, bool subdirCheck = false)
            {
                return CommonLib.GetAssetBundleNameListFromPath(path, subdirCheck);
            }

            public static void UnloadBundles()
            {
                var bundles = LoadedBundles.ToArray();
                LoadedBundles.Clear();
                foreach (var assetBundle in bundles)
                {
                    AssetBundleManager.UnloadAssetBundle(assetBundle, false);
                }
            }

#if HS
            public static string[] GetAssetNamesFromBundle(string assetBundleName) => AssetBundleCheck.GetAllAssetName(assetBundleName);
#else
            public static string[] GetAssetNamesFromBundle(string assetBundleName)
            {
                try
                {
                    return AssetBundleCheck.GetAllAssetName(assetBundleName);
                }
                catch { }


                try
                {
                    return AssetBundleCheck.GetAllAssetName(assetBundleName, isAllCheck: true);
                }
                catch { }


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
                                    return loadedAssetBundle.m_AssetBundle.GetAllAssetNames().Select(Path.GetFileName)
                                        .ToArray();
                                }
                            }
                        }
                    }
                }
                catch { }

                AssetBundleManager.LoadAssetBundleInternal(assetBundleName, false);
                var loadedAssetBundle1 = AssetBundleManager.GetLoadedAssetBundle(assetBundleName, out var err);
                AssetBundle assetBundle;
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
                    if (!(assetBundle is null))
                    {
                        return assetBundle.GetAllAssetNames().Select(Path.GetFileName).ToArray();
                    }

                    if (err.IsNullOrEmpty())
                    {
                        throw new NullReferenceException($"Unable to load assetBundle {assetBundleName}");
                    }
                    else
                    {
                        throw new NullReferenceException($"Unable to load assetBundle {assetBundleName}: {err}");
                    }
                }
                finally
                {
                    if (loadedAssetBundle1 == null && assetBundle != null)
                    {
                        AssetBundleManager.UnloadAssetBundle(assetBundle.name, false);
                    }
                }
            }
#endif

            public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : Object
            {
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
                try
                {
                    var result = CommonLib.LoadAsset<T>(bundle, asset, false, manifest);
                    AssetBundleManager.UnloadAssetBundle(bundle, true, manifest);
                    return result;
                }
                catch
                {
                    AssetBundleManager.LoadAssetBundleInternal(bundle, false, manifest);
                    var assetBundle = AssetBundleManager.GetLoadedAssetBundle(bundle, out var error, manifest);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Logger?.LogError($"ManualLoadAsset: {error}");
                    }

                    var result = assetBundle.m_AssetBundle.LoadAsset<T>(asset);
                    return result;
                }
#endif
#endif
            }
        }
    }
}
