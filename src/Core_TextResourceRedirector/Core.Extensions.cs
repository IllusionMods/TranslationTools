using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public static class Extensions
    {
        public static Dictionary<string, string> BuildReverseDictionary(this SimpleTextTranslationCache cache)
        {
            var translations = Traverse.Create(cache).Field<Dictionary<string, string>>("_translations").Value;
            var reverse = new Dictionary<string, string>();
            foreach (var entry in translations)
            {
                reverse[entry.Value] = entry.Key;
            }

            return reverse;
        }

        public static bool TryGetReverseTranslation(this SimpleTextTranslationCache cache, string translatedText,
            out string result)
        {
            return BuildReverseDictionary(cache).TryGetValue(translatedText, out result);
        }

        public static string DefaultCalculateModificationFilePath<TAsset>(this TAsset asset, IAssetOrResourceLoadedContext context)
            where TAsset : UnityEngine.Object
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        internal static bool DefaultShouldHandleAsset<THandler, TAsset>(this THandler handler, TAsset asset,
            IAssetOrResourceLoadedContext context) 
            where TAsset: UnityEngine.Object
            where THandler: IRedirectorHandler<TAsset>
        {
            return handler.Enabled && !context.HasReferenceBeenRedirectedBefore(asset);
        }

        public static ConfigEntry<TConf> ConfigEntryBind<TConf>(this IRedirectorHandler handler, string key, TConf defaultValue, string description)
        {
            return handler.Plugin.Config.Bind<TConf>(handler.ConfigSectionName, key, defaultValue, description);
        }

        public static ConfigEntry<TConf> ConfigEntryBind<TConf>(this IRedirectorHandler handler, string key, TConf defaultValue, ConfigDescription description)
        {
            return handler.Plugin.Config.Bind<TConf>(handler.ConfigSectionName, key, defaultValue, description);
        }
    }
}
