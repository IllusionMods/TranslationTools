using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using Illusion.Extensions;
using IllusionMods.Shared;
using UnityEngine;
using static IllusionMods.TextResourceHelper.Helpers;

#if LOCALIZE
using Localize.Translate;
#endif

namespace IllusionMods
{
    public partial class BaseDumpHelper
    {
        protected const string NewlineReplaceValue = "\\n";

        protected static readonly Regex NewlineReplaceRegex =
            new Regex(Regex.Escape("\n"),
                Constants.DefaultRegexOptions);

        private ManualLogSource _logger;

        public BaseDumpHelper(TextDump plugin)
        {
            Plugin = plugin;
        }

        protected ManualLogSource Logger => _logger = _logger ?? BepInEx.Logging.Logger.CreateLogSource(GetType().Name);

        protected TextDump Plugin { get; }

        protected TextResourceHelper ResourceHelper => Plugin?.TextResourceHelper;
        protected TextAssetTableHelper TableHelper => Plugin?.TextResourceHelper?.TableHelper;

        protected static string BuildSeenKey(int topLevel, string tag)
        {
            return JoinStrings("_", topLevel.ToString(), tag);
        }

#if LOCALIZE
        protected static string BuildSeenKey(int topLevel, Data.Param param)
        {
            return JoinStrings("_",
                topLevel.ToString(),
                !string.IsNullOrEmpty(param.tag) ? param.tag : param.ID.ToString());
        }
#endif

        public void AddLocalizationToResults(IDictionary<string, string> results, string origText, string transText)
        {
            ResourceHelper.AddLocalizationToResults(results, origText, transText);
        }

        public void AddLocalizationToResults(IDictionary<string, string> results, KeyValuePair<string, string> mapping)
        {
            ResourceHelper.AddLocalizationToResults(results, mapping);
        }

        public bool IsValidLocalization(string original, string localization)
        {
            return ResourceHelper.IsValidLocalization(original, localization);
        }

        public static TranslationGenerator WrapTranslationCollector(string path,
            TranslationDumper<IDictionary<string, string>>.TranslationCollector translationCollector)
        {
            IEnumerable<ITranslationDumper> Generator()
            {
                yield return new StringTranslationDumper(path, translationCollector);
            }

            return Generator;
        }

        public static List<string> GetAllAssetBundleNames()
        {
            return TextDump.Helpers.GetAllAssetBundleNames();
        }

        public static List<string> GetAssetBundleNameListFromPath(string path, bool subdirCheck = false)
        {
            return TextDump.Helpers.GetAssetBundleNameListFromPath(path, subdirCheck);
        }

        public static void UnloadBundles()
        {
            TextDump.Helpers.UnloadBundles();
        }

        public static string[] GetAssetNamesFromBundle(string assetBundleName)
        {
            return TextDump.Helpers.GetAssetNamesFromBundle(assetBundleName);
        }

        public static T ManualLoadAsset<T>(string bundle, string asset, string manifest) where T : Object
        {
            return TextDump.Helpers.ManualLoadAsset<T>(bundle, asset, manifest);
        }

        public static T ManualLoadAsset<T>(AssetBundleAddress assetBundleAddress) where T : Object
        {
            return TextDump.Helpers.ManualLoadAsset<T>(assetBundleAddress);
        }

        public virtual void PrepareLineForDump(ref string key, ref string value)
        {
            key = NewlineReplaceRegex.Replace(key, NewlineReplaceValue);
            value = NewlineReplaceRegex.Replace(value, NewlineReplaceValue);
        }

        protected static List<GameObject> GetChildrenFromGameObject(GameObject parent)
        {
#if AI
            return parent.Children();
#else
            var gameObjects = new List<GameObject>();
            for (var i = 0; i < parent.transform.childCount; i++)
            {
                gameObjects.Add(parent.transform.GetChild(i).gameObject);
            }
            return gameObjects;
#endif
        }
    }
}
