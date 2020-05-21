using System.IO;
using System.Linq;
using ActionGame;
using BepInEx.Logging;
using XUnity.AutoTranslator.Plugin.Core;
using XUnity.AutoTranslator.Plugin.Core.AssetRedirection;
using XUnity.AutoTranslator.Plugin.Core.Utilities;
using XUnity.ResourceRedirector;

namespace IllusionMods
{
    public class EventInfoHandler : AssetLoadedHandlerBaseV2<EventInfo>
    {
        private readonly TextResourceHelper _textResourceHelper;

        public EventInfoHandler(TextResourceHelper helper)
        {
            CheckDirectory = true;
            _textResourceHelper = helper;
            Logger.LogDebug($"{GetType()} enabled");
        }

        private static ManualLogSource Logger => TextResourceRedirector.Logger;

        protected override bool ReplaceOrUpdateAsset(string calculatedModificationPath, ref EventInfo asset,
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

            var result = false;
            foreach (var entry in asset.param)
            {
                var key = _textResourceHelper.GetSpecializedKey(entry, entry.Name);
                if (string.IsNullOrEmpty(key)) continue;
                if (cache.TryGetTranslation(key, true, out var translated))
                {
                    entry.Name = translated;
                    TranslationHelper.RegisterRedirectedResourceTextToPath(translated, calculatedModificationPath);
                    result = true;
                }
                else if (AutoTranslatorSettings.IsDumpingRedirectedResourcesEnabled &&
                         LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, !string.IsNullOrEmpty(entry.Name) ? entry.Name : string.Empty);
                }
            }

            return result;
        }

        protected override bool DumpAsset(string calculatedModificationPath, EventInfo asset,
            IAssetOrResourceLoadedContext context)
        {
            var defaultTranslationFile = Path.Combine(calculatedModificationPath, "translation.txt");
            var cache = new SimpleTextTranslationCache(
                defaultTranslationFile,
                false);

            foreach (var entry in asset.param)
            {
                var key = _textResourceHelper.GetSpecializedKey(entry, entry.Name);
                var value = !string.IsNullOrEmpty(entry.Name) ? entry.Name : string.Empty;
                if (!string.IsNullOrEmpty(key) && LanguageHelper.IsTranslatable(key))
                {
                    cache.AddTranslationToCache(key, value);
                }
            }

            return true;
        }

        protected override string CalculateModificationFilePath(EventInfo asset, IAssetOrResourceLoadedContext context)
        {
            return context.GetPreferredFilePathWithCustomFileName(asset, null).Replace(".unity3d", "");
        }

        protected override bool ShouldHandleAsset(EventInfo asset, IAssetOrResourceLoadedContext context)
        {
            return !context.HasReferenceBeenRedirectedBefore(asset);
        }
    }
}
