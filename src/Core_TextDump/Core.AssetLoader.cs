using System;
using System.Collections.Generic;
using System.Linq;
using ADV.Commands.Base;
using BepInEx.Logging;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace IllusionMods
{
    internal static partial class AssetLoader
    {
        private static readonly HashSet<string> LoadedBundles = new HashSet<string>();

        private static ManualLogSource Logger => TextDump.Logger;
        public static void UnloadBundles()
        {
            var bundles = LoadedBundles.ToList();
            LoadedBundles.Clear();
            foreach (var assetBundle in bundles)
            {
                AssetBundleManager.UnloadAssetBundle(assetBundle, false);
            }
        }

        private static bool TryLoader<T>(Func<T> loader, string loaderName, string assetBundle, string assetName,
            bool forceUnload, out T result)
            where T : UnityEngine.Object
        {
            result = default;
            var loaderMsg = $"{assetBundle}/{assetName} via {loaderName}";
            if (forceUnload) loaderMsg += " (forced unload)";

            try
            {
                Logger.DebugLogDebug($"AssetLoader: trying to load {loaderMsg}");
                if (forceUnload)
                {
                    try
                    {
                        AssetBundleManager.UnloadAssetBundle(assetBundle, false);
                    }
                    catch { }
                }

                result = loader();
            }
            catch (Exception err)
            {
                Logger.LogDebug(
                    $"AssetLoader: unable to load {loaderMsg}: {err}");
            }

            if (result == default) return false;
            LoadedBundles.Add(assetBundle);
            Logger.DebugLogDebug(
                $"AssetLoader: loaded {loaderMsg}: {result?.GetType()}");
            return true;
        }

        public static T ManualLoadAsset<T>(string assetBundle, string assetName, string manifest = null)
            where T : UnityEngine.Object
        {
            T result = default;
            if (!AssetBundleCheck.IsFile(assetBundle))
            {
                Logger.LogWarning($"AssetLoader: No such asset bundle: {assetBundle}");
                return result;
            }

            manifest = string.IsNullOrEmpty(manifest) ? null : manifest;
            var loaders = new Loader<T>.AssetBundleLoader[]
            {
#if AI
                Loader<T>.AssetUtilityLoader,
#endif
#if HS2
                Loader<T>.AddObjectAssistLoader,
#endif
                Loader<T>.CommonLibLoader,
#if AI || HS2
                Loader<T>.AssetBundleManagerLoadAssetLoader,
#endif
                Loader<T>.AssetBundleManagerLoader,
#if HS2
                Loader<T>.AssetBundleDataLoader,
#endif
                Loader<T>.DefaultLoader
            };
            foreach (var forceUnload in new[] {false/*, true*/})
            {
                foreach (var loader in loaders)
                {
                    if (TryLoader<T>(() => loader(assetBundle, assetName, manifest), loader.Method.Name, assetBundle,
                        assetName, forceUnload, out result))
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        public static T ManualLoadAsset<T>(AssetBundleAddress assetBundleAddress) where T : UnityEngine.Object
        {
            T result = default;
            if (!AssetBundleCheck.IsFile(assetBundleAddress.AssetBundle))
            {
                Logger.LogWarning($"AssetLoader: No such asset bundle: {assetBundleAddress.AssetBundle}");
                return result;
            }
            var loaders = new Loader<T>.AssetBundleAddressLoader[]
            {
#if AI || HS2
                Loader<T>.AddressAsInfoLoader,
#else
                Loader<T>.DefaultLoader
#endif
            };

            foreach (var forceUnload in new[] {false, true})
            {
                foreach (var loader in loaders)
                {
                    
                    if (TryLoader<T>(() => loader(assetBundleAddress), loader.Method.Name, assetBundleAddress.AssetBundle,
                        assetBundleAddress.Asset, forceUnload,
                        out result))
                    {
                        return result;
                    }
                }
            }

            return result;
        }


        internal static partial class Loader<T> where T : UnityEngine.Object
        {
           internal static T CommonLibLoader(string assetBundle, string assetName, string manifest)
            {
                var result = CommonLib.LoadAsset<T>(assetBundle, assetName, false, manifest);
                return result;
            }

            internal static T DefaultLoader(AssetBundleAddress assetBundleAddress)
            {
                return ManualLoadAsset<T>(assetBundleAddress.AssetBundle, assetBundleAddress.Asset,
                    assetBundleAddress.Manifest);
            }

            internal static T DefaultLoader(string assetBundle, string assetName, string manifest)
            {
                return default;
            }

            internal static T AssetBundleManagerLoader(string assetBundleName, string assetName, string manifest)
            {
                var bundleLoader =
#if HS2
                    AssetBundleManager.LoadAssetBundle(assetBundleName, manifest);
#else
                    AssetBundleManager.LoadAssetBundle(assetBundleName, false, manifest);
#endif
                if (bundleLoader == null) return null;
                var bundle =
#if HS2
                    bundleLoader.Bundle;
#else
                    bundleLoader.m_AssetBundle;
#endif
                if (bundle == null) return null;
                
                if (typeof(ScriptableObject).IsAssignableFrom(typeof(T)))
                {
                    // try and force script to be loaded to fix HS2 issue
                    try
                    {
                        var dummy = ScriptableObject.CreateInstance(typeof(T));
                    }
                    catch (Exception err)
                    {
                        Logger.LogFatal(err);
                    }
                }
                return bundle.LoadAsset<T>(assetName);

            }

            internal delegate T AssetBundleAddressLoader(AssetBundleAddress assetBundleAddress);

            internal delegate T AssetBundleLoader(string assetBundle, string assetName, string manifest);
        }
    }
}
