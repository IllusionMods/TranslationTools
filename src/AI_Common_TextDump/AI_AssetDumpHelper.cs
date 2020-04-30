using System.Collections.Generic;
using System.IO;

namespace IllusionMods
{
    public class AI_AssetDumpHelper : AssetDumpHelper
    {
        protected readonly AssetDumpColumnInfo ItemLookup;
        protected readonly AssetDumpColumnInfo ItemLookupAndAssetCols;
        protected readonly AssetDumpColumnInfo TitleAssetCols;

        public AI_AssetDumpHelper(TextDump plugin) : base(plugin)
        {
            TitleAssetCols = new AssetDumpColumnInfo {CombineWithParentBundle = true};

            ItemLookup = new AssetDumpColumnInfo(null, null, true, new[]
            {
                "アイテム名",
                "名前(メモ)"
            });

            ItemLookupAndAssetCols =
                new AssetDumpColumnInfo(null, StdExcelAssetCols.NameMappings, true, ItemLookup.ItemLookupColumns);

            ListEntryDumpers.Add(TryDumpTitleSkillName);
        }

        protected virtual bool TryDumpTitleSkillName(string assetBundleName, string assetName, AssetDumpColumnInfo _,
            IDictionary<string, string> translations)
        {
            var titleSkillName = ManualLoadAsset<TitleSkillName>(assetBundleName, assetName, "abdata");
            if (titleSkillName is null)
            {
                return false;
            }

            foreach (var entry in titleSkillName.param)
            {
                var key = entry.name0;
                var value = entry.name1;
                ResourceHelper.AddLocalizationToResults(translations, key, value);
            }

            LocalizationDumpHelper?.AddAutoLocalizer(
                $"{assetBundleName.Replace(".unity3d", string.Empty)}/{Path.GetFileNameWithoutExtension(assetName)}",
                new Dictionary<string, string>(translations));
            return true;
        }

        /*
        private IEnumerable<KeyValuePair<string, string>> HandleChaListData(UnityEngine.TextAsset asset, AssetDumpColumnInfo assetDumpColumnInfo)
        {
            ChaListData chaListData = MessagePackSerializer.Deserialize<ChaListData>(asset.bytes);
            foreach (var entry in chaListData.dictList.Values)
            {
                foreach (var mapping in assetDumpColumnInfo.NumericMappings)
                {
                    var jpCol = mapping.Key;
                    var transCol = mapping.Value;
                    if (entry.Count < jpCol)
                    {
                        continue;
                    }
                    var key = entry[jpCol];
                    var val = string.Empty;
                    if (transCol >= 0 && entry.Count > Math.Max(11, transCol))
                    {
                        val = entry[transCol];
                    }
                    ///Logger.LogWarning($"match: {key}={val}");
                    yield return new KeyValuePair<string, string>(key, val);
                }
            }

            foreach (var id in chaListData.dictList.Keys)
            {
                foreach (var mapping in assetDumpColumnInfo.NameMappings)
                {
                    var key = chaListData.GetInfo(id, mapping.Key);
                    if (!string.IsNullOrEmpty(key))
                    {
                        var val = chaListData.GetInfo(id, mapping.Value);
                        yield return new KeyValuePair<string, string>(key, IsValidLocalization(key, val) ? val : string.Empty);
                    }
                }
            }
        }

        */

        protected override IEnumerable<KeyValuePair<string, AssetDumpColumnInfo>> GetLists()
        {
            foreach (var list in base.GetLists())
            {
                yield return list;
            }

            yield return new KeyValuePair<string, AssetDumpColumnInfo>("actor/animal", ItemLookupAndAssetCols);

            foreach (var animal in new[] {"cat", "chicken"})
            {
                foreach (var state in new[] {"wild", "pet"})
                {
                    yield return new KeyValuePair<string, AssetDumpColumnInfo>($"actor/animal/action/{animal}/{state}",
                        StdExcelAssetCols);
                }
            }

            yield return new KeyValuePair<string, AssetDumpColumnInfo>("actor/gameitem/recipe/recycling", ItemLookup);

            foreach (var mapdir in new[] {"eventpoint", "popupinfo"})
            {
                yield return new KeyValuePair<string, AssetDumpColumnInfo>($"map/{mapdir}", StdExcelAssetCols);
            }
        }
    }
}
