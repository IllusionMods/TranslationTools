using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IllusionMods.Shared;
using Manager;

namespace IllusionMods
{
    public class AI_HS2_AssetDumpHelper : AssetDumpHelper 
    {
        protected AssetDumpColumnInfo ItemLookup;
        protected AssetDumpColumnInfo ItemLookupAndAssetCols;

        protected AI_HS2_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            ItemLookup = new AssetDumpColumnInfo(null, null, true, new[]
            {
                "アイテム名",
                "名前(メモ)",
            });

            ItemLookupAndAssetCols =
                new AssetDumpColumnInfo(null, StdExcelAssetCols.NameMappings, true, ItemLookup.ItemLookupColumns);

        }

        public override void InitializeHelper()
        {
            AssetDumpGenerators.Insert(0, GetVoiceInfoDumpers);
            AssetDumpGenerators.Add(GetHPositionDumpers);
            base.InitializeHelper();
        }

        protected virtual IEnumerable<string> GetHTypeNames()
        {
            return new[] {"aibu", "houshi", "sonyu", "tokushu", "les", "3P_F2M1", "3P"};
        }

        protected virtual IEnumerable<ITranslationDumper> GetHPositionDumpers()
        {
            const string rootPath = "list/h/animationinfo";

            var hTypeNames = GetHTypeNames();

            bool IsHPositionList(string assetName)
            {
                return hTypeNames.Any(n => assetName.StartsWith($"{n}_", StringComparison.OrdinalIgnoreCase));
            }

            var assetDumpColumnInfo = new AssetDumpColumnInfo(new KeyValuePair<int, int>(0, -1));

            foreach (var assetBundleName in GetAssetBundleNameListFromPath(rootPath, true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    if (!IsHPositionList(assetName)) continue;


                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();
                        var done = false;
                        foreach (var tryDump in ListEntryDumpers)
                        {
                            GetAssetBundleNameListFromPath(rootPath, true);
                            GetAssetNamesFromBundle(assetBundleName);

                            done = tryDump(assetBundleName, assetName, assetDumpColumnInfo, translations);
                            if (done) break;
                        }

                        if (!done) Logger.LogWarning($"Unable to dump '{rootPath}': '{assetBundleName}': {assetName}");

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }

        protected override bool IsValidExcelLocalization(string assetBundleName, string assetName, int firstRow,
            int row, string origString,
            string possibleTranslation)
        {
            // HS2 already has translation columns in table (mostly empty)
            // row 0 is always 'None' which isn't always correct
            if (possibleTranslation == "None" && origString != "なし" && (row == 0 || row == firstRow)) return false;
            return base.IsValidExcelLocalization(assetBundleName, assetName, row, firstRow, origString,
                possibleTranslation);
        }

        protected override bool IsValidChaListDataLocalization(int id, List<string> entry, string origString,
            string possibleTranslation)
        {
            // HS2 already has translation columns in table (mostly empty)
            // row 0 is always 'None' which isn't always correct
            if (possibleTranslation == "None" && origString != "なし" && id == 0) return false;

            return base.IsValidChaListDataLocalization(id, entry, origString, possibleTranslation);
        }

        protected virtual string GetParamEntryTranslation(object paramEntry)
        {
            return string.Empty;
        }

        protected IEnumerable<ITranslationDumper> GetVoiceInfoDumpers()
        {
            foreach (var assetBundleName in GetAssetBundleNameListFromPath("etcetra/list/config/", true))
            {
                foreach (var assetName in GetAssetNamesFromBundle(assetBundleName))
                {
                    var filePath = BuildAssetFilePath(assetBundleName, assetName);

                    IDictionary<string, string> AssetDumper()
                    {
                        var translations = new OrderedDictionary<string, string>();

                        var asset = ManualLoadAsset<VoiceInfo>(assetBundleName, assetName, "abdata");
                        if (asset is null) return translations;

                        foreach (var entry in asset.param)
                        {
                            AddLocalizationToResults(translations, entry.Personality, GetParamEntryTranslation(entry));
                        }

                        return translations;
                    }

                    yield return new StringTranslationDumper(filePath, AssetDumper);
                }
            }
        }
    }
}
