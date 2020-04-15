using BepInEx.Logging;
using System.Collections.Generic;

namespace IllusionMods
{
    public partial class BaseDumpHelper
    {
        protected ManualLogSource _logger = null;
        protected ManualLogSource Logger => _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(GetType().Name);

        protected TextDump Plugin { get; }
        public BaseDumpHelper(TextDump plugin) : base()
        {
            this.Plugin = plugin;
        }

        protected TextResourceHelper ResourceHelper => Plugin?.textResourceHelper;
        protected TextAssetTableHelper TableHelper => Plugin?.textAssetTableHelper;

        public void AddLocalizationToResults(Dictionary<string, string> results, string origText, string transText) =>
            ResourceHelper.AddLocalizationToResults(results, origText, transText);
        public void AddLocalizationToResults(Dictionary<string, string> results, KeyValuePair<string, string> mapping) =>
            ResourceHelper.AddLocalizationToResults(results, mapping);
        public bool IsValidLocalization(string original, string localization) => ResourceHelper.IsValidLocalization(original, localization);
        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : UnityEngine.Object
        {
            return TextResourceHelper.ManualLoadAsset<T>(bundle, asset, manifest);
        }

        public static string CombinePaths(params string[] parts) => TextResourceHelper.CombinePaths(parts);
        public static void UnloadBundles() => TextResourceHelper.UnloadBundles();

        public static List<string> GetAllAssetBundles() => CommonLib.GetAssetBundleNameListFromPath(".", true);

        public static TranslationGenerator WrapTranslationCollector(string path, TranslationCollector translationCollector)
        {
            IEnumerable<TranslationDumper> generator()
            {
                yield return new TranslationDumper(path, translationCollector);
            }
            return generator;
        }
    }
}
