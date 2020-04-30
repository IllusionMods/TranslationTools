using System.IO;
using System.Linq;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class TitleSkillNameHandler : AssetLoadedHandlerBaseV2<TitleSkillName>
    {
        public TitleSkillNameHandler()
        {
            CheckDirectory = true;
            Logger.LogDebug($"{GetType()} enabled");
        }

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        protected override string CalculateModificationFilePath(TitleSkillName asset,
            IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool DumpAsset(string calculatedModificationPath, TitleSkillName asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            foreach (var entry in asset.param)
            {
                var key = entry.name0;
                var value = !string.IsNullOrEmpty(entry.name1) ? entry.name1 : entry.name0;
                if (!string.IsNullOrEmpty(key) && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, value);
                }
            }

            return true;
        }

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref TitleSkillName asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var redirectedResources = RedirectedDirectory.GetFilesInDirectory(calculatedModificationPath, ".txt");
            var streams = redirectedResources.Select(x => x.OpenStream());
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                streams,
                false,
                true);

            if (cache.IsEmpty) return false;

            foreach (var entry in asset.param)
            {
                var key = entry.name0;
                if (string.IsNullOrEmpty(key)) continue;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    entry.name0 = translated;
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, !string.IsNullOrEmpty(entry.name1) ? entry.name1 : key);
                }
            }

            return true;
        }

        protected override bool ShouldHandleAsset(TitleSkillName asset, IAssetOrResourceLoadedContext context)
        {
            return !context.HasReferenceBeenRedirectedBefore(asset);
        }
    }
}
